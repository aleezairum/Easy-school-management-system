using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Http;
using Google.Apis.Services;
using Google.Apis.Upload;
using School.API.DTOs.Settings;
using School.API.Services.Interfaces.Settings;

namespace School.API.Services.Implementations.Settings
{
    // Uploads the latest local backup to Google Drive and prunes older uploads,
    // supporting either a Service Account (unattended, recommended for this
    // scheduled job) or an OAuth user credential seeded from a stored refresh
    // token — chosen per BackupJobSettingDto.DriveAuthMode.
    public class GoogleDriveBackupUploader : IGoogleDriveBackupUploader
    {
        private const string FolderMimeType = "application/vnd.google-apps.folder";

        private readonly ILogger<GoogleDriveBackupUploader> _logger;

        public GoogleDriveBackupUploader(ILogger<GoogleDriveBackupUploader> logger)
        {
            _logger = logger;
        }

        public async Task<BackupRunResultDto> UploadAndApplyRetentionAsync(
            string filePath, BackupJobSettingDto settings, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(settings.DriveFolderPath))
                return new BackupRunResultDto { Success = false, Message = "Google Drive folder path is not configured." };

            var isServiceAccount = !string.Equals(settings.DriveAuthMode, "OAuth", StringComparison.OrdinalIgnoreCase);

            try
            {
                var credential = BuildCredential(settings);
                using var driveService = new DriveService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "EasySchool DB Backup"
                });

                var folderId = await ResolveOrCreateFolderPathAsync(
                    driveService, settings.DriveFolderPath!, isServiceAccount, ct);

                var fileName = Path.GetFileName(filePath);
                var fileMetadata = new Google.Apis.Drive.v3.Data.File
                {
                    Name = fileName,
                    Parents = new List<string> { folderId }
                };

                await using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var createRequest = driveService.Files.Create(fileMetadata, stream, "application/octet-stream");
                    createRequest.Fields = "id, name, createdTime";
                    var uploadProgress = await createRequest.UploadAsync(ct);
                    if (uploadProgress.Status != UploadStatus.Completed)
                    {
                        return new BackupRunResultDto
                        {
                            Success = false,
                            Message = $"Google Drive upload failed: {uploadProgress.Exception?.Message}"
                        };
                    }
                }

                var deleted = await ApplyRetentionAsync(driveService, folderId, settings.DriveRetentionDays, ct);

                return new BackupRunResultDto
                {
                    Success = true,
                    Message = $"Uploaded '{fileName}' to Google Drive ({settings.DriveFolderPath}). Removed {deleted} backup(s) older than {settings.DriveRetentionDays} day(s)."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GoogleDriveBackupUploader] Upload failed");
                return new BackupRunResultDto { Success = false, Message = $"Google Drive upload failed: {ex.Message}" };
            }
        }

        // Walks / creates the folder path (e.g. "Backups/EasySchool") and returns the
        // final folder's ID.
        //
        // OAuth mode: the path is resolved (and created if missing) starting from
        // the signed-in user's own Drive root — they own the storage quota.
        //
        // Service Account mode: a bare service account has no Drive storage quota
        // of its own, so the FIRST path segment must already exist and be shared
        // with the service account's email (Editor access) — we search for it by
        // name instead of assuming a "root" the service account doesn't have.
        // Segments after that are created under it as needed (allowed by the
        // inherited Editor permission).
        private static async Task<string> ResolveOrCreateFolderPathAsync(
            DriveService driveService, string path, bool isServiceAccount, CancellationToken ct)
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length == 0)
                throw new InvalidOperationException("Google Drive folder path is empty.");

            string? parentId;
            int startIndex;

            if (isServiceAccount)
            {
                var topFolderId = await FindFolderAsync(driveService, segments[0], null, ct);
                if (topFolderId == null)
                {
                    throw new InvalidOperationException(
                        $"Folder '{segments[0]}' was not found. In Service Account mode, first share this top-level " +
                        "folder with the service account's email (Editor access) in Google Drive.");
                }
                parentId = topFolderId;
                startIndex = 1;
            }
            else
            {
                parentId = "root";
                startIndex = 0;
            }

            for (var i = startIndex; i < segments.Length; i++)
            {
                var existingId = await FindFolderAsync(driveService, segments[i], parentId, ct);
                if (existingId != null)
                {
                    parentId = existingId;
                    continue;
                }

                var folder = new Google.Apis.Drive.v3.Data.File
                {
                    Name = segments[i],
                    MimeType = FolderMimeType,
                    Parents = new List<string> { parentId! }
                };
                var createRequest = driveService.Files.Create(folder);
                createRequest.Fields = "id";
                var created = await createRequest.ExecuteAsync(ct);
                parentId = created.Id;
            }

            return parentId!;
        }

        private static async Task<string?> FindFolderAsync(
            DriveService driveService, string name, string? parentId, CancellationToken ct)
        {
            var escapedName = name.Replace("\\", "\\\\").Replace("'", "\\'");
            var q = $"name = '{escapedName}' and mimeType = '{FolderMimeType}' and trashed = false";
            if (parentId != null) q += $" and '{parentId}' in parents";

            var listRequest = driveService.Files.List();
            listRequest.Q = q;
            listRequest.Fields = "files(id, name)";
            listRequest.PageSize = 10;
            var result = await listRequest.ExecuteAsync(ct);
            return result.Files?.FirstOrDefault()?.Id;
        }

        private static async Task<int> ApplyRetentionAsync(
            DriveService driveService, string folderId, int retentionDays, CancellationToken ct)
        {
            var cutoffUtc = DateTime.UtcNow.AddDays(-Math.Max(retentionDays, 0));

            var listRequest = driveService.Files.List();
            listRequest.Q = $"'{folderId}' in parents and trashed = false";
            listRequest.Fields = "files(id, name, createdTime)";
            listRequest.PageSize = 1000;
            var files = await listRequest.ExecuteAsync(ct);

            var deleted = 0;
            foreach (var file in files.Files ?? new List<Google.Apis.Drive.v3.Data.File>())
            {
                if (file.CreatedTimeDateTimeOffset.HasValue &&
                    file.CreatedTimeDateTimeOffset.Value.UtcDateTime < cutoffUtc)
                {
                    await driveService.Files.Delete(file.Id).ExecuteAsync(ct);
                    deleted++;
                }
            }

            return deleted;
        }

        private static IConfigurableHttpClientInitializer BuildCredential(BackupJobSettingDto settings)
        {
            if (string.Equals(settings.DriveAuthMode, "OAuth", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(settings.OAuthClientId) ||
                    string.IsNullOrWhiteSpace(settings.OAuthClientSecret) ||
                    string.IsNullOrWhiteSpace(settings.OAuthRefreshToken))
                {
                    throw new InvalidOperationException(
                        "OAuth mode requires the app to be connected to Google Drive first (use the " +
                        "\"Connect with Google\" button on the Backup settings page).");
                }

                var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = settings.OAuthClientId,
                        ClientSecret = settings.OAuthClientSecret
                    },
                    Scopes = new[] { DriveService.Scope.DriveFile }
                });

                var token = new TokenResponse { RefreshToken = settings.OAuthRefreshToken };
                return new UserCredential(flow, "db-backup-job", token);
            }

            // Service Account (default / recommended for an unattended job).
            if (string.IsNullOrWhiteSpace(settings.ServiceAccountKeyPath) || !File.Exists(settings.ServiceAccountKeyPath))
            {
                throw new InvalidOperationException(
                    $"Service account key file not found: {settings.ServiceAccountKeyPath}");
            }

            return GoogleCredential.FromFile(settings.ServiceAccountKeyPath)
                .CreateScoped(DriveService.Scope.DriveFile);
        }
    }
}

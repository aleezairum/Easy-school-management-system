using Microsoft.Data.SqlClient;
using School.API.Data.DBModels.Settings;
using School.API.DTOs.Common;
using School.API.DTOs.Settings;
using School.API.Repositories.Interfaces.Settings;
using School.API.Services.Interfaces.Settings;

namespace School.API.Services.Implementations.Settings
{
    public class BackupJobSettingService : IBackupJobSettingService
    {
        private readonly IBackupJobSettingRepository _repo;
        private readonly IGoogleDriveBackupUploader _driveUploader;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BackupJobSettingService> _logger;

        public BackupJobSettingService(
            IBackupJobSettingRepository repo,
            IGoogleDriveBackupUploader driveUploader,
            IConfiguration configuration,
            ILogger<BackupJobSettingService> logger)
        {
            _repo = repo;
            _driveUploader = driveUploader;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<BackupJobSettingDto?> GetAsync()
        {
            var s = await _repo.GetAsync();
            if (s == null) return null;

            // Public-facing copy: never send secrets back to the browser. The
            // page only needs to know a secret/token IS set, not what it is.
            var dto = ToDto(s);
            dto.OAuthClientSecret = null;
            dto.OAuthRefreshToken = null;
            return dto;
        }

        public Task<ResponseDto> SaveAsync(BackupJobSettingSaveDto dto, int userId, string userIp)
            => _repo.SaveAsync(
                dto.IsEnabled, dto.SelectedDatabasesCsv, dto.BackupFolder, dto.RunTimesCsv, dto.LocalRetentionDays,
                dto.DriveEnabled, dto.DriveAuthMode, dto.DriveFolderPath, dto.DriveRetentionDays,
                dto.ServiceAccountKeyPath, dto.OAuthClientId, dto.OAuthClientSecret, dto.OAuthRefreshToken,
                userId, userIp);

        public Task SaveOAuthRefreshTokenAsync(string refreshToken)
            => _repo.SaveOAuthRefreshTokenAsync(refreshToken);

        public List<AvailableDatabaseDto> GetAvailableDatabases()
        {
            var result = new List<AvailableDatabaseDto>();
            var section = _configuration.GetSection("ConnectionStrings");

            foreach (var child in section.GetChildren())
            {
                if (string.IsNullOrWhiteSpace(child.Value)) continue;

                string databaseName;
                try
                {
                    databaseName = new SqlConnectionStringBuilder(child.Value).InitialCatalog;
                }
                catch
                {
                    continue; // not a valid SQL connection string — skip it
                }

                if (string.IsNullOrWhiteSpace(databaseName)) continue;

                result.Add(new AvailableDatabaseDto { Name = child.Key, DatabaseName = databaseName });
            }

            return result;
        }

        public async Task<BackupRunResultDto> RunBackupNowAsync(CancellationToken ct)
        {
            var settingsModel = await _repo.GetAsync();
            if (settingsModel == null)
                return new BackupRunResultDto { Success = false, Message = "Backup settings are not configured." };

            var settings = ToDto(settingsModel);
            var folder = string.IsNullOrWhiteSpace(settings.BackupFolder)
                ? @"D:\Backups\EasySchool\EasySchoolDB"
                : settings.BackupFolder!;

            var databases = GetAvailableDatabases();
            var selectedNames = (settings.SelectedDatabasesCsv ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            // Nothing explicitly selected yet: back up every configured database
            // (matches the settings page, which shows all checked by default).
            var targets = selectedNames.Count > 0
                ? databases.Where(d => selectedNames.Contains(d.Name)).ToList()
                : databases;

            if (targets.Count == 0)
            {
                var noDb = new BackupRunResultDto { Success = false, Message = "No database is selected to back up." };
                await _repo.RecordRunResultAsync(false, noDb.Message);
                return noDb;
            }

            var allSucceeded = true;
            var messageParts = new List<string>();

            foreach (var db in targets)
            {
                string filePath;
                try
                {
                    filePath = await _repo.ExecuteBackupAsync(db.DatabaseName, folder);
                    _logger.LogInformation("[DbBackupJob] Backup created for {Db} at {Path}", db.DatabaseName, filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[DbBackupJob] SQL backup failed for {Db}", db.DatabaseName);
                    allSucceeded = false;
                    messageParts.Add($"{db.DatabaseName}: backup failed — {ex.Message}");
                    continue;
                }

                messageParts.Add($"{db.DatabaseName}: backup created ({Path.GetFileName(filePath)}).");

                if (settings.DriveEnabled)
                {
                    var driveResult = await _driveUploader.UploadAndApplyRetentionAsync(filePath, settings, ct);
                    messageParts.Add(driveResult.Message);
                    if (!driveResult.Success) allSucceeded = false;
                }
            }

            var localDeleted = CleanupLocalOldBackups(folder, settings.LocalRetentionDays);
            if (localDeleted > 0)
                messageParts.Add($"Removed {localDeleted} local backup(s) older than {settings.LocalRetentionDays} day(s).");

            var result = new BackupRunResultDto { Success = allSucceeded, Message = string.Join(' ', messageParts) };
            await _repo.RecordRunResultAsync(allSucceeded, result.Message);
            return result;
        }

        public Task<List<BackupFileInfoDto>> GetLocalBackupFilesAsync()
        {
            return GetLocalBackupFilesInternalAsync();
        }

        private async Task<List<BackupFileInfoDto>> GetLocalBackupFilesInternalAsync()
        {
            var settingsModel = await _repo.GetAsync();
            var folder = settingsModel?.BackupFolder;
            var result = new List<BackupFileInfoDto>();

            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                return result;

            foreach (var file in new DirectoryInfo(folder).GetFiles("*.bak"))
            {
                result.Add(new BackupFileInfoDto
                {
                    FileName = file.Name,
                    SizeBytes = file.Length,
                    CreatedDate = file.CreationTime
                });
            }

            return result.OrderByDescending(f => f.CreatedDate).ToList();
        }

        // Deletes .bak files older than retentionDays from the local backup folder.
        // Assumes the API process can reach this path (same machine as SQL Server,
        // or a UNC path both can read/write) — same assumption the backup SP makes.
        private int CleanupLocalOldBackups(string folder, int retentionDays)
        {
            if (!Directory.Exists(folder) || retentionDays <= 0)
                return 0;

            var cutoff = DateTime.Now.AddDays(-retentionDays);
            var deleted = 0;

            foreach (var file in new DirectoryInfo(folder).GetFiles("*.bak"))
            {
                if (file.CreationTime >= cutoff) continue;
                try
                {
                    file.Delete();
                    deleted++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[DbBackupJob] Could not delete old backup {File}", file.FullName);
                }
            }

            return deleted;
        }

        private static BackupJobSettingDto ToDto(BackupJobSetting s) => new()
        {
            Id = s.Id,
            IsEnabled = s.IsEnabled,
            SelectedDatabasesCsv = s.SelectedDatabasesCsv,
            BackupFolder = s.BackupFolder,
            RunTimesCsv = s.RunTimesCsv,
            LocalRetentionDays = s.LocalRetentionDays,
            DriveEnabled = s.DriveEnabled,
            DriveAuthMode = s.DriveAuthMode,
            DriveFolderPath = s.DriveFolderPath,
            DriveRetentionDays = s.DriveRetentionDays,
            ServiceAccountKeyPath = s.ServiceAccountKeyPath,
            OAuthClientId = s.OAuthClientId,
            // Secrets are used server-side only and never round-tripped to the browser.
            OAuthClientSecret = s.OAuthClientSecret,
            OAuthRefreshToken = s.OAuthRefreshToken,
            OAuthConnected = !string.IsNullOrWhiteSpace(s.OAuthRefreshToken),
            LastRunAt = s.LastRunAt,
            LastRunSuccess = s.LastRunSuccess,
            LastRunMessage = s.LastRunMessage,
            UpdatedBy = s.UpdatedBy,
            UpdatedDate = s.UpdatedDate,
            UpdatedIp = s.UpdatedIp
        };
    }
}

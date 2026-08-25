using School.API.DTOs.Settings;

namespace School.API.Services.Interfaces.Settings
{
    public interface IGoogleDriveBackupUploader
    {
        // Uploads filePath into settings.DriveFolderId, then deletes files in that
        // same Drive folder older than settings.DriveRetentionDays.
        Task<BackupRunResultDto> UploadAndApplyRetentionAsync(
            string filePath, BackupJobSettingDto settings, CancellationToken ct);
    }
}

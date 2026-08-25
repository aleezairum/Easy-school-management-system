using School.API.Data.DBModels.Settings;
using School.API.DTOs.Common;

namespace School.API.Repositories.Interfaces.Settings
{
    public interface IBackupJobSettingRepository
    {
        Task<BackupJobSetting?> GetAsync();

        Task<ResponseDto> SaveAsync(
            bool isEnabled, string? selectedDatabasesCsv, string backupFolder, string runTimesCsv, int localRetentionDays,
            bool driveEnabled, string driveAuthMode, string? driveFolderPath, int driveRetentionDays,
            string? serviceAccountKeyPath, string? oAuthClientId, string? oAuthClientSecret, string? oAuthRefreshToken,
            int userId, string userIp);

        // Runs dbo.SpGet_DBBackup for one database and returns the full path of the .bak file it created.
        Task<string> ExecuteBackupAsync(string databaseName, string backupFolder);

        Task RecordRunResultAsync(bool success, string message);

        // Saves the refresh token obtained from the in-app "Connect with Google" OAuth flow.
        Task SaveOAuthRefreshTokenAsync(string refreshToken);
    }
}

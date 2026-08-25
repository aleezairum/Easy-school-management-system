using School.API.DTOs.Common;
using School.API.DTOs.Settings;

namespace School.API.Services.Interfaces.Settings
{
    public interface IBackupJobSettingService
    {
        Task<BackupJobSettingDto?> GetAsync();
        Task<ResponseDto> SaveAsync(BackupJobSettingSaveDto dto, int userId, string userIp);

        // Runs the backup now (used by both the "Run Now" button and the
        // scheduled background job): SQL backup (for each selected database)
        // -> local retention cleanup -> optional Google Drive upload + Drive-side
        // retention -> records result.
        Task<BackupRunResultDto> RunBackupNowAsync(CancellationToken ct);

        Task<List<BackupFileInfoDto>> GetLocalBackupFilesAsync();

        // Databases available to select, sourced from appsettings.json ConnectionStrings.
        List<AvailableDatabaseDto> GetAvailableDatabases();

        // Persists the refresh token obtained from the "Connect with Google" OAuth flow.
        Task SaveOAuthRefreshTokenAsync(string refreshToken);
    }
}

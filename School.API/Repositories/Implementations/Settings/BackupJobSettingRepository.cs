using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.Settings;
using School.API.DTOs.Common;
using School.API.Repositories.Interfaces.Settings;

namespace School.API.Repositories.Implementations.Settings
{
    public class BackupJobSettingRepository : IBackupJobSettingRepository
    {
        private readonly SchoolDbContext _context;

        public BackupJobSettingRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<BackupJobSetting?> GetAsync()
        {
            var rows = await _context.Set<BackupJobSetting>()
                .FromSqlRaw("EXEC SpGet_BackupJobSetting")
                .AsNoTracking()
                .ToListAsync();
            return rows.FirstOrDefault();
        }

        public async Task<ResponseDto> SaveAsync(
            bool isEnabled, string? selectedDatabasesCsv, string backupFolder, string runTimesCsv, int localRetentionDays,
            bool driveEnabled, string driveAuthMode, string? driveFolderPath, int driveRetentionDays,
            string? serviceAccountKeyPath, string? oAuthClientId, string? oAuthClientSecret, string? oAuthRefreshToken,
            int userId, string userIp)
        {
            var rows = await _context.Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpSave_BackupJobSetting @IsEnabled={0}, @SelectedDatabasesCsv={1}, @BackupFolder={2}, @RunTimesCsv={3}, " +
                    "@LocalRetentionDays={4}, @DriveEnabled={5}, @DriveAuthMode={6}, @DriveFolderPath={7}, " +
                    "@DriveRetentionDays={8}, @ServiceAccountKeyPath={9}, @OAuthClientId={10}, " +
                    "@OAuthClientSecret={11}, @OAuthRefreshToken={12}, @UserID={13}, @UserIP={14}",
                    isEnabled, (object?)selectedDatabasesCsv ?? DBNull.Value, backupFolder, runTimesCsv, localRetentionDays,
                    driveEnabled, driveAuthMode, (object?)driveFolderPath ?? DBNull.Value, driveRetentionDays,
                    (object?)serviceAccountKeyPath ?? DBNull.Value, (object?)oAuthClientId ?? DBNull.Value,
                    (object?)oAuthClientSecret ?? DBNull.Value, (object?)oAuthRefreshToken ?? DBNull.Value,
                    userId, userIp)
                .AsNoTracking()
                .ToListAsync();
            return rows.First();
        }

        public async Task<string> ExecuteBackupAsync(string databaseName, string backupFolder)
        {
            var path = await _context.Database
                .SqlQueryRaw<string>(
                    "EXEC SpGet_DBBackup @BackupFolder={0}, @DatabaseName={1}", backupFolder, databaseName)
                .ToListAsync();
            return path.First();
        }

        public async Task RecordRunResultAsync(bool success, string message)
        {
            var conn = _context.Database.GetDbConnection();
            var shouldClose = conn.State != System.Data.ConnectionState.Open;
            if (shouldClose) await conn.OpenAsync();
            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "EXEC SpSave_BackupJobRunResult @Success, @Message";
                cmd.Parameters.Add(new SqlParameter("@Success", success));
                cmd.Parameters.Add(new SqlParameter("@Message", (object?)message ?? DBNull.Value));
                await cmd.ExecuteNonQueryAsync();
            }
            finally
            {
                if (shouldClose) await conn.CloseAsync();
            }
        }

        public async Task SaveOAuthRefreshTokenAsync(string refreshToken)
        {
            var conn = _context.Database.GetDbConnection();
            var shouldClose = conn.State != System.Data.ConnectionState.Open;
            if (shouldClose) await conn.OpenAsync();
            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "EXEC SpSave_BackupJobOAuthToken @OAuthRefreshToken";
                cmd.Parameters.Add(new SqlParameter("@OAuthRefreshToken", refreshToken));
                await cmd.ExecuteNonQueryAsync();
            }
            finally
            {
                if (shouldClose) await conn.CloseAsync();
            }
        }
    }
}

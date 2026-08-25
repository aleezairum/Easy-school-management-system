namespace School.API.DTOs.Settings
{
    // Returned to the Backup settings page: current configuration + last-run status.
    public class BackupJobSettingDto
    {
        public int Id { get; set; }

        public bool IsEnabled { get; set; }
        public string? SelectedDatabasesCsv { get; set; }
        public string? BackupFolder { get; set; }
        public string? RunTimesCsv { get; set; }
        public int LocalRetentionDays { get; set; }

        public bool DriveEnabled { get; set; }
        public string? DriveAuthMode { get; set; }
        public string? DriveFolderPath { get; set; }
        public int DriveRetentionDays { get; set; }

        public string? ServiceAccountKeyPath { get; set; }
        public string? OAuthClientId { get; set; }
        public string? OAuthClientSecret { get; set; }
        public string? OAuthRefreshToken { get; set; }
        public bool OAuthConnected { get; set; }

        public DateTime? LastRunAt { get; set; }
        public bool? LastRunSuccess { get; set; }
        public string? LastRunMessage { get; set; }

        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedIp { get; set; }
    }

    // Posted from the Backup settings page to save configuration.
    public class BackupJobSettingSaveDto
    {
        public bool IsEnabled { get; set; }
        public string? SelectedDatabasesCsv { get; set; }
        public string BackupFolder { get; set; } = "";
        public string RunTimesCsv { get; set; } = "";
        public int LocalRetentionDays { get; set; } = 30;

        public bool DriveEnabled { get; set; }
        public string DriveAuthMode { get; set; } = "ServiceAccount";
        public string? DriveFolderPath { get; set; }
        public int DriveRetentionDays { get; set; } = 7;

        public string? ServiceAccountKeyPath { get; set; }
        public string? OAuthClientId { get; set; }
        public string? OAuthClientSecret { get; set; }
        // Left blank on a normal Save — the refresh token is only ever set by
        // the "Connect with Google" OAuth flow, never typed in by hand.
        public string? OAuthRefreshToken { get; set; }
    }

    // One local .bak file, for the "recent backups" list on the settings page.
    public class BackupFileInfoDto
    {
        public string FileName { get; set; } = "";
        public long SizeBytes { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    // Result of a manual or scheduled backup run.
    public class BackupRunResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }

    // One selectable database, sourced from appsettings.json ConnectionStrings.
    public class AvailableDatabaseDto
    {
        public string Name { get; set; } = "";         // ConnectionStrings key, e.g. "DefaultConnection"
        public string DatabaseName { get; set; } = "";  // Actual SQL Server database name (Initial Catalog)
    }
}

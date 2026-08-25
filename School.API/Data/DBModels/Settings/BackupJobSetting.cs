namespace School.API.Data.DBModels.Settings
{
    // Single-row (Id = 1) configuration for the scheduled DB backup job.
    // Backed by dbo.BackupJobSetting (Scripts/BackupJobSetting.sql).
    public class BackupJobSetting
    {
        public int Id { get; set; }

        public bool IsEnabled { get; set; }
        public string? SelectedDatabasesCsv { get; set; }
        public string BackupFolder { get; set; } = "";
        public string RunTimesCsv { get; set; } = "";
        public int LocalRetentionDays { get; set; }

        public bool DriveEnabled { get; set; }
        public string DriveAuthMode { get; set; } = "ServiceAccount";
        public string? DriveFolderPath { get; set; }
        public int DriveRetentionDays { get; set; }

        public string? ServiceAccountKeyPath { get; set; }
        public string? OAuthClientId { get; set; }
        public string? OAuthClientSecret { get; set; }
        public string? OAuthRefreshToken { get; set; }

        public DateTime? LastRunAt { get; set; }
        public bool? LastRunSuccess { get; set; }
        public string? LastRunMessage { get; set; }

        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedIp { get; set; }
    }
}

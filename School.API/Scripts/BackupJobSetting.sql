-- =============================================
-- DB Backup Job — settings table + stored procedures
-- ----------------------------------------------------------------------------
-- Single-row configuration (Id = 1) for the scheduled database backup job:
-- one or more selected databases (sourced from appsettings.json
-- ConnectionStrings) + a local folder + up to 3 daily run times + local
-- retention (days), plus an optional upload of each run's backups to a
-- Google Drive folder PATH (Service Account or OAuth) with its own,
-- separate retention (days).
--
-- The actual backup is performed by dbo.SpGet_DBBackup (Scripts/SpGet_DBBackup.sql).
-- This script only stores/serves the configuration and the last-run status
-- shown on the Backup settings page.
-- =============================================

-- Table (fresh install schema)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BackupJobSetting')
BEGIN
    CREATE TABLE [dbo].[BackupJobSetting] (
        [Id]                     INT            NOT NULL PRIMARY KEY,

        [IsEnabled]              BIT            NOT NULL DEFAULT 0,
        [SelectedDatabasesCsv]   NVARCHAR(500)  NULL,   -- ConnectionStrings key names, e.g. "DefaultConnection"
        [BackupFolder]           NVARCHAR(400)  NOT NULL DEFAULT 'D:\Backups\EasySchool\EasySchoolDB',
        [RunTimesCsv]            NVARCHAR(100)  NOT NULL DEFAULT '06:00,14:00,22:00',
        [LocalRetentionDays]     INT            NOT NULL DEFAULT 30,

        [DriveEnabled]           BIT            NOT NULL DEFAULT 0,
        [DriveAuthMode]          NVARCHAR(20)   NOT NULL DEFAULT 'ServiceAccount',  -- 'ServiceAccount' | 'OAuth'
        [DriveFolderPath]        NVARCHAR(400)  NULL,   -- e.g. "EasySchool Backups" or "Backups/EasySchool"
        [DriveRetentionDays]     INT            NOT NULL DEFAULT 7,

        [ServiceAccountKeyPath]  NVARCHAR(400)  NULL,
        [OAuthClientId]          NVARCHAR(200)  NULL,
        [OAuthClientSecret]      NVARCHAR(200)  NULL,
        [OAuthRefreshToken]      NVARCHAR(400)  NULL,

        [LastRunAt]              DATETIME2      NULL,
        [LastRunSuccess]         BIT            NULL,
        [LastRunMessage]         NVARCHAR(1000) NULL,

        [UpdatedBy]              INT            NULL,
        [UpdatedDate]            DATETIME2      NULL,
        [UpdatedIp]              NVARCHAR(50)   NULL,

        CONSTRAINT [CK_BackupJobSetting_SingleRow] CHECK ([Id] = 1)
    );

    INSERT INTO [dbo].[BackupJobSetting] (Id) VALUES (1);
END
GO

-- Migrate an existing install (created before Drive-folder-path / multi-database
-- support existed) to the current schema.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BackupJobSetting') AND name = 'DriveFolderPath')
BEGIN
    ALTER TABLE [dbo].[BackupJobSetting] ADD [DriveFolderPath] NVARCHAR(400) NULL;
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BackupJobSetting') AND name = 'DriveFolderId')
BEGIN
    -- Carry the old raw folder ID forward so existing configuration isn't silently
    -- lost — it will be re-resolved as a "path" the next time the job runs, and
    -- the admin can replace it with a human-readable path from the settings page.
    -- Run via EXEC() so the column reference only binds if this branch is reached
    -- (once dropped below, later re-runs of this script must not fail to compile).
    EXEC('UPDATE [dbo].[BackupJobSetting] SET DriveFolderPath = DriveFolderId WHERE DriveFolderPath IS NULL AND DriveFolderId IS NOT NULL');
    ALTER TABLE [dbo].[BackupJobSetting] DROP COLUMN [DriveFolderId];
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BackupJobSetting') AND name = 'SelectedDatabasesCsv')
BEGIN
    ALTER TABLE [dbo].[BackupJobSetting] ADD [SelectedDatabasesCsv] NVARCHAR(500) NULL;
END
GO

-- =============================================
-- SpGet_BackupJobSetting: the single configuration row
-- =============================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'SpGet_BackupJobSetting')
    DROP PROCEDURE SpGet_BackupJobSetting;
GO

CREATE PROCEDURE [dbo].[SpGet_BackupJobSetting]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, IsEnabled, SelectedDatabasesCsv, BackupFolder, RunTimesCsv, LocalRetentionDays,
           DriveEnabled, DriveAuthMode, DriveFolderPath, DriveRetentionDays,
           ServiceAccountKeyPath, OAuthClientId, OAuthClientSecret, OAuthRefreshToken,
           LastRunAt, LastRunSuccess, LastRunMessage,
           UpdatedBy, UpdatedDate, UpdatedIp
    FROM   [dbo].[BackupJobSetting]
    WHERE  Id = 1;
END
GO

-- =============================================
-- SpSave_BackupJobSetting: update the configuration row
-- =============================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'SpSave_BackupJobSetting')
    DROP PROCEDURE SpSave_BackupJobSetting;
GO

CREATE PROCEDURE [dbo].[SpSave_BackupJobSetting]
(
    @IsEnabled             BIT,
    @SelectedDatabasesCsv  NVARCHAR(500)  = NULL,
    @BackupFolder          NVARCHAR(400),
    @RunTimesCsv           NVARCHAR(100),
    @LocalRetentionDays    INT,
    @DriveEnabled          BIT,
    @DriveAuthMode         NVARCHAR(20),
    @DriveFolderPath       NVARCHAR(400)  = NULL,
    @DriveRetentionDays    INT,
    @ServiceAccountKeyPath NVARCHAR(400)  = NULL,
    @OAuthClientId         NVARCHAR(200)  = NULL,
    @OAuthClientSecret     NVARCHAR(200)  = NULL,
    @OAuthRefreshToken     NVARCHAR(400)  = NULL,
    @UserID                INT,
    @UserIP                NVARCHAR(50)
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE [dbo].[BackupJobSetting]
    SET IsEnabled             = @IsEnabled,
        SelectedDatabasesCsv  = @SelectedDatabasesCsv,
        BackupFolder          = @BackupFolder,
        RunTimesCsv           = @RunTimesCsv,
        LocalRetentionDays    = @LocalRetentionDays,
        DriveEnabled          = @DriveEnabled,
        DriveAuthMode         = @DriveAuthMode,
        DriveFolderPath       = @DriveFolderPath,
        DriveRetentionDays    = @DriveRetentionDays,
        ServiceAccountKeyPath = @ServiceAccountKeyPath,
        OAuthClientId         = @OAuthClientId,
        -- Secrets are never sent back to the browser, so the page always posts
        -- them blank unless the admin is actively changing them — only
        -- overwrite when a non-blank value is actually supplied.
        OAuthClientSecret     = COALESCE(NULLIF(@OAuthClientSecret, ''), OAuthClientSecret),
        -- The refresh token is saved separately by the OAuth "Connect" flow
        -- (SpSave_BackupJobOAuthToken), so a plain settings Save must not blank it out.
        OAuthRefreshToken     = COALESCE(NULLIF(@OAuthRefreshToken, ''), OAuthRefreshToken),
        UpdatedBy             = @UserID,
        UpdatedDate           = GETUTCDATE(),
        UpdatedIp             = @UserIP
    WHERE Id = 1;

    SELECT CAST(1 AS DECIMAL) AS VID, 0 AS ReturnCode, 'Backup job settings saved successfully' AS ReturnMessage;
END
GO

-- =============================================
-- SpSave_BackupJobOAuthToken: stores the refresh token obtained from the
-- in-app "Connect with Google" (OAuth) flow.
-- =============================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'SpSave_BackupJobOAuthToken')
    DROP PROCEDURE SpSave_BackupJobOAuthToken;
GO

CREATE PROCEDURE [dbo].[SpSave_BackupJobOAuthToken]
(
    @OAuthRefreshToken NVARCHAR(400)
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE [dbo].[BackupJobSetting]
    SET OAuthRefreshToken = @OAuthRefreshToken,
        DriveAuthMode     = 'OAuth'
    WHERE Id = 1;
END
GO

-- =============================================
-- SpSave_BackupJobRunResult: records the outcome of the most recent run,
-- shown as the "Last Run" status on the Backup settings page.
-- =============================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'SpSave_BackupJobRunResult')
    DROP PROCEDURE SpSave_BackupJobRunResult;
GO

CREATE PROCEDURE [dbo].[SpSave_BackupJobRunResult]
(
    @Success BIT,
    @Message NVARCHAR(1000)
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE [dbo].[BackupJobSetting]
    SET LastRunAt     = GETUTCDATE(),
        LastRunSuccess = @Success,
        LastRunMessage = @Message
    WHERE Id = 1;
END
GO

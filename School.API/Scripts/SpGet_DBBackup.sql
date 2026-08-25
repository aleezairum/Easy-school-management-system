USE [EasySchoolDB]
GO
/* ============================================================================
   SpGet_DBBackup
   ----------------------------------------------------------------------------
   Backs up @DatabaseName (any database on this SQL Server instance — not just
   EasySchoolDB, so the Backup settings page can back up multiple selected
   databases) to a .bak file inside @BackupFolder, and returns the full path
   of the file that was created (BackupFilePath), so the caller (the DB Backup
   background job) knows exactly what to upload / clean up.

   The target folder is created server-side via xp_create_subdir if it does
   not already exist, so this works whether the caller (API process) and SQL
   Server share a machine or not — @BackupFolder only needs to be reachable
   from the SQL Server service account.

   The database name is validated against sys.databases and applied via
   QUOTENAME + sp_executesql (BACKUP DATABASE does not accept a parameter for
   the database name directly), which also protects against SQL injection.

   Not using WITH COMPRESSION: backup compression requires SQL Server
   Standard/Enterprise/Developer edition and is unavailable on Express, so it
   is left off for broad compatibility across deployments.

   Parameters:
     @BackupFolder NVARCHAR(400) = NULL  - target folder (default when NULL/blank:
                                            D:\Backups\EasySchool\EasySchoolDB)
     @DatabaseName NVARCHAR(128) = NULL  - database to back up (default when
                                            NULL/blank: EasySchoolDB)
   ============================================================================ */
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'SpGet_DBBackup')
    DROP PROCEDURE SpGet_DBBackup;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SpGet_DBBackup]
(
    @BackupFolder NVARCHAR(400) = NULL,
    @DatabaseName NVARCHAR(128) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @BackupFolder IS NULL OR LTRIM(RTRIM(@BackupFolder)) = ''
        SET @BackupFolder = 'D:\Backups\EasySchool\EasySchoolDB';

    IF @DatabaseName IS NULL OR LTRIM(RTRIM(@DatabaseName)) = ''
        SET @DatabaseName = 'EasySchoolDB';

    IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = @DatabaseName)
    BEGIN
        RAISERROR('Database "%s" does not exist on this server.', 16, 1, @DatabaseName);
        RETURN;
    END

    IF RIGHT(@BackupFolder, 1) <> '\'
        SET @BackupFolder = @BackupFolder + '\';

    EXEC master.dbo.xp_create_subdir @BackupFolder;

    DECLARE @FileName NVARCHAR(500) =
        @BackupFolder + @DatabaseName + '_' +
        CONVERT(VARCHAR(8), GETDATE(), 112) + '_' +
        REPLACE(CONVERT(VARCHAR(8), GETDATE(), 108), ':', '') + '.bak';

    DECLARE @Sql NVARCHAR(MAX) =
        N'BACKUP DATABASE ' + QUOTENAME(@DatabaseName) + N' TO DISK = @p_FileName WITH INIT, STATS = 100;';

    EXEC sp_executesql @Sql, N'@p_FileName NVARCHAR(500)', @p_FileName = @FileName;

    SELECT @FileName AS BackupFilePath;
END
GO

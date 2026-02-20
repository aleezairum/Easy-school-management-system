-- =============================================
-- Campus Table and Stored Procedures
-- =============================================

-- Create Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Campus]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Campus] (
        [VID]          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [VName]        NVARCHAR(100) NOT NULL,
        [IsActive]     BIT NOT NULL DEFAULT 1,
        [InsertedBy]   INT NULL,
        [InsertedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [InsertedIp]   NVARCHAR(50) NULL,
        [UpdatedBy]    INT NULL,
        [UpdatedDate]  DATETIME2 NULL,
        [UpdatedIp]    NVARCHAR(50) NULL,
        CONSTRAINT [UQ_Campus_VName] UNIQUE ([VName])
    );
END
GO

-- =============================================
-- SpGet_Campus: Get all or by ID
-- =============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SpGet_Campus]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[SpGet_Campus]
GO

CREATE PROCEDURE [dbo].[SpGet_Campus]
    @VID INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF @VID = 0
    BEGIN
        SELECT VID, VName, IsActive, InsertedBy, InsertedDate, InsertedIp, UpdatedBy, UpdatedDate, UpdatedIp
        FROM [dbo].[Campus]
        ORDER BY VID DESC;
    END
    ELSE
    BEGIN
        SELECT VID, VName, IsActive, InsertedBy, InsertedDate, InsertedIp, UpdatedBy, UpdatedDate, UpdatedIp
        FROM [dbo].[Campus]
        WHERE VID = @VID;
    END
END
GO

-- =============================================
-- SpSave_Campus: Insert or Update
-- =============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SpSave_Campus]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[SpSave_Campus]
GO

CREATE PROCEDURE [dbo].[SpSave_Campus]
    @VID INT = 0,
    @VName NVARCHAR(100),
    @IsActive BIT = 1,
    @UserID INT = NULL,
    @UserIP NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Check for duplicate name
    IF EXISTS (SELECT 1 FROM [dbo].[Campus] WHERE VName = @VName AND VID <> @VID)
    BEGIN
        SELECT @VID AS VID, -1 AS ReturnCode, 'Campus name already exists' AS ReturnMessage;
        RETURN;
    END

    IF @VID = 0
    BEGIN
        -- Insert
        INSERT INTO [dbo].[Campus] (VName, IsActive, InsertedBy, InsertedDate, InsertedIp)
        VALUES (@VName, @IsActive, @UserID, GETUTCDATE(), @UserIP);

        SET @VID = SCOPE_IDENTITY();

        SELECT @VID AS VID, 0 AS ReturnCode, 'Campus saved successfully' AS ReturnMessage;
    END
    ELSE
    BEGIN
        -- Update
        UPDATE [dbo].[Campus]
        SET VName = @VName,
            IsActive = @IsActive,
            UpdatedBy = @UserID,
            UpdatedDate = GETUTCDATE(),
            UpdatedIp = @UserIP
        WHERE VID = @VID;

        SELECT @VID AS VID, 0 AS ReturnCode, 'Campus updated successfully' AS ReturnMessage;
    END
END
GO

-- =============================================
-- SpDelete_Campus: Delete by ID
-- =============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SpDelete_Campus]') AND type in (N'P'))
    DROP PROCEDURE [dbo].[SpDelete_Campus]
GO

CREATE PROCEDURE [dbo].[SpDelete_Campus]
    @VID INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Campus] WHERE VID = @VID)
    BEGIN
        SELECT @VID AS VID, -1 AS ReturnCode, 'Campus not found' AS ReturnMessage;
        RETURN;
    END

    DELETE FROM [dbo].[Campus] WHERE VID = @VID;

    SELECT @VID AS VID, 0 AS ReturnCode, 'Campus deleted successfully' AS ReturnMessage;
END
GO

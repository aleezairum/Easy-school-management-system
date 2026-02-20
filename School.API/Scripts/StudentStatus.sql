-- =============================================
-- Student Status Table & Stored Procedures
-- =============================================

-- Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StudentStatus')
BEGIN
    CREATE TABLE [dbo].[StudentStatus] (
        [VID]          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [VName]        NVARCHAR(100) NOT NULL,
        [IsActive]     BIT NOT NULL DEFAULT 1,
        [InsertedBy]   INT NULL,
        [InsertedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [InsertedIp]   NVARCHAR(50) NULL,
        [UpdatedBy]    INT NULL,
        [UpdatedDate]  DATETIME2 NULL,
        [UpdatedIp]    NVARCHAR(50) NULL,
        CONSTRAINT [UQ_StudentStatus_VName] UNIQUE ([VName])
    );
END
GO

-- =============================================
-- SpGet_StudentStatus: Get all or by ID
-- =============================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'SpGet_StudentStatus')
    DROP PROCEDURE SpGet_StudentStatus;
GO

CREATE PROCEDURE [dbo].[SpGet_StudentStatus]
    @VID INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF @VID = 0
    BEGIN
        SELECT VID, VName, IsActive, InsertedBy, InsertedDate, InsertedIp, UpdatedBy, UpdatedDate, UpdatedIp
        FROM StudentStatus
        ORDER BY VName;
    END
    ELSE
    BEGIN
        SELECT VID, VName, IsActive, InsertedBy, InsertedDate, InsertedIp, UpdatedBy, UpdatedDate, UpdatedIp
        FROM StudentStatus
        WHERE VID = @VID;
    END
END
GO

-- =============================================
-- SpSave_StudentStatus: Insert or Update
-- =============================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'SpSave_StudentStatus')
    DROP PROCEDURE SpSave_StudentStatus;
GO

CREATE PROCEDURE [dbo].[SpSave_StudentStatus]
    @VID      INT,
    @VName    NVARCHAR(100),
    @IsActive BIT,
    @UserID   INT,
    @UserIP   NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    -- Check for duplicate name
    IF EXISTS (SELECT 1 FROM StudentStatus WHERE VName = @VName AND VID <> @VID)
    BEGIN
        SELECT CAST(0 AS DECIMAL) AS VID, -1 AS ReturnCode, 'Student status name already exists' AS ReturnMessage;
        RETURN;
    END

    IF @VID = 0
    BEGIN
        -- Insert
        INSERT INTO StudentStatus (VName, IsActive, InsertedBy, InsertedDate, InsertedIp)
        VALUES (@VName, @IsActive, @UserID, GETUTCDATE(), @UserIP);

        SET @VID = SCOPE_IDENTITY();
        SELECT CAST(@VID AS DECIMAL) AS VID, 0 AS ReturnCode, 'Student status saved successfully' AS ReturnMessage;
    END
    ELSE
    BEGIN
        -- Update
        UPDATE StudentStatus
        SET VName = @VName,
            IsActive = @IsActive,
            UpdatedBy = @UserID,
            UpdatedDate = GETUTCDATE(),
            UpdatedIp = @UserIP
        WHERE VID = @VID;

        SELECT CAST(@VID AS DECIMAL) AS VID, 0 AS ReturnCode, 'Student status updated successfully' AS ReturnMessage;
    END
END
GO

-- =============================================
-- SpDelete_StudentStatus: Delete by ID
-- =============================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'SpDelete_StudentStatus')
    DROP PROCEDURE SpDelete_StudentStatus;
GO

CREATE PROCEDURE [dbo].[SpDelete_StudentStatus]
    @VID INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM StudentStatus WHERE VID = @VID)
    BEGIN
        SELECT CAST(0 AS DECIMAL) AS VID, -1 AS ReturnCode, 'Student status not found' AS ReturnMessage;
        RETURN;
    END

    DELETE FROM StudentStatus WHERE VID = @VID;
    SELECT CAST(@VID AS DECIMAL) AS VID, 0 AS ReturnCode, 'Student status deleted successfully' AS ReturnMessage;
END
GO

-- =============================================
-- Fee Type - Add Frequency Column & Update Stored Procedures
-- =============================================

-- Add Frequency column if it doesn't exist
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'FeeTypes')
   AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('FeeTypes') AND name = 'Frequency')
BEGIN
    ALTER TABLE FeeTypes ADD Frequency NVARCHAR(20) NOT NULL DEFAULT 'Monthly';
END
GO

-- =============================================
-- SpGet_FeeType: Get all or by ID
-- =============================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'SpGet_FeeType')
    DROP PROCEDURE SpGet_FeeType;
GO

CREATE PROCEDURE SpGet_FeeType
    @VID INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF @VID = 0
    BEGIN
        SELECT VID, VName, Frequency, IsActive, InsertedBy, InsertedDate, InsertedIp, UpdatedBy, UpdatedDate, UpdatedIp
        FROM FeeTypes
        ORDER BY VID DESC;
    END
    ELSE
    BEGIN
        SELECT VID, VName, Frequency, IsActive, InsertedBy, InsertedDate, InsertedIp, UpdatedBy, UpdatedDate, UpdatedIp
        FROM FeeTypes
        WHERE VID = @VID;
    END
END
GO

-- =============================================
-- SpSave_FeeType: Insert or Update
-- =============================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'SpSave_FeeType')
    DROP PROCEDURE SpSave_FeeType;
GO

CREATE PROCEDURE SpSave_FeeType
    @VID INT = 0,
    @VName NVARCHAR(100),
    @Frequency NVARCHAR(20) = 'Monthly',
    @IsActive BIT = 1,
    @UserID INT = NULL,
    @UserIP NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Check for duplicate name
    IF EXISTS (SELECT 1 FROM FeeTypes WHERE VName = @VName AND VID <> @VID)
    BEGIN
        SELECT CAST(0 AS DECIMAL) AS VID, -1 AS ReturnCode, 'Fee type name already exists' AS ReturnMessage;
        RETURN;
    END

    IF @VID = 0
    BEGIN
        -- Insert
        INSERT INTO FeeTypes (VName, Frequency, IsActive, InsertedBy, InsertedDate, InsertedIp)
        VALUES (@VName, @Frequency, @IsActive, @UserID, GETUTCDATE(), @UserIP);

        SET @VID = SCOPE_IDENTITY();

        SELECT CAST(@VID AS DECIMAL) AS VID, 0 AS ReturnCode, 'Fee type saved successfully' AS ReturnMessage;
    END
    ELSE
    BEGIN
        -- Update
        UPDATE FeeTypes
        SET VName = @VName,
            Frequency = @Frequency,
            IsActive = @IsActive,
            UpdatedBy = @UserID,
            UpdatedDate = GETUTCDATE(),
            UpdatedIp = @UserIP
        WHERE VID = @VID;

        SELECT CAST(@VID AS DECIMAL) AS VID, 0 AS ReturnCode, 'Fee type updated successfully' AS ReturnMessage;
    END
END
GO

-- =============================================
-- SpDelete_FeeType: Delete by ID
-- =============================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'SpDelete_FeeType')
    DROP PROCEDURE SpDelete_FeeType;
GO

CREATE PROCEDURE SpDelete_FeeType
    @VID INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM FeeTypes WHERE VID = @VID)
    BEGIN
        SELECT CAST(0 AS DECIMAL) AS VID, -1 AS ReturnCode, 'Fee type not found' AS ReturnMessage;
        RETURN;
    END

    DELETE FROM FeeTypes WHERE VID = @VID;

    SELECT CAST(@VID AS DECIMAL) AS VID, 0 AS ReturnCode, 'Fee type deleted successfully' AS ReturnMessage;
END
GO

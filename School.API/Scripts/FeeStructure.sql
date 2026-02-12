-- ===
-- Fee Structure Table & Stored Procedures
-- ===

-- 1. Create Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FeeStructures')
BEGIN
    CREATE TABLE FeeStructures (
        VID INT IDENTITY(1,1) PRIMARY KEY,
        AcademicSessionID INT NOT NULL,
        ClassID INT NOT NULL,
        GradeID INT NOT NULL,
        FeeTypeID INT NOT NULL,
        Amount DECIMAL(18,2) NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        InsertedBy INT NULL,
        InsertedDate DATETIME NOT NULL DEFAULT GETUTCDATE(),
        InsertedIp NVARCHAR(50) NULL,
        UpdatedBy INT NULL,
        UpdatedDate DATETIME NULL,
        UpdatedIp NVARCHAR(50) NULL,
        CONSTRAINT FK_FeeStructures_AcademicSession FOREIGN KEY (AcademicSessionID) REFERENCES AcademicSessionYears(VID),
        CONSTRAINT FK_FeeStructures_Class FOREIGN KEY (ClassID) REFERENCES SMSClasses(VID),
        CONSTRAINT FK_FeeStructures_Grade FOREIGN KEY (GradeID) REFERENCES AcademicGrades(Id),
        CONSTRAINT FK_FeeStructures_FeeType FOREIGN KEY (FeeTypeID) REFERENCES FeeTypes(VID),
        CONSTRAINT UQ_FeeStructures_Combination UNIQUE (AcademicSessionID, ClassID, GradeID, FeeTypeID)
    );
END
GO

-- 2. Get Stored Procedure
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'SpGet_FeeStructure')
    DROP PROCEDURE SpGet_FeeStructure;
GO

CREATE PROCEDURE SpGet_FeeStructure
    @VID INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF @VID = 0
    BEGIN
        SELECT
            fs.VID,
            fs.AcademicSessionID,
            fs.ClassID,
            fs.GradeID,
            fs.FeeTypeID,
            fs.Amount,
            fs.IsActive,
            asy.VName AS AcademicSessionName,
            c.VName AS ClassName,
            ag.Name AS GradeName,
            ft.VName AS FeeTypeName,
            fs.InsertedBy,
            fs.InsertedDate,
            fs.InsertedIp,
            fs.UpdatedBy,
            fs.UpdatedDate,
            fs.UpdatedIp
        FROM FeeStructures fs
        INNER JOIN AcademicSessionYears asy ON fs.AcademicSessionID = asy.VID
        INNER JOIN SMSClasses c ON fs.ClassID = c.VID
        INNER JOIN AcademicGrades ag ON fs.GradeID = ag.Id
        INNER JOIN FeeTypes ft ON fs.FeeTypeID = ft.VID
        ORDER BY fs.VID DESC;
    END
    ELSE
    BEGIN
        SELECT
            fs.VID,
            fs.AcademicSessionID,
            fs.ClassID,
            fs.GradeID,
            fs.FeeTypeID,
            fs.Amount,
            fs.IsActive,
            asy.VName AS AcademicSessionName,
            c.VName AS ClassName,
            ag.Name AS GradeName,
            ft.VName AS FeeTypeName,
            fs.InsertedBy,
            fs.InsertedDate,
            fs.InsertedIp,
            fs.UpdatedBy,
            fs.UpdatedDate,
            fs.UpdatedIp
        FROM FeeStructures fs
        INNER JOIN AcademicSessionYears asy ON fs.AcademicSessionID = asy.VID
        INNER JOIN SMSClasses c ON fs.ClassID = c.VID
        INNER JOIN AcademicGrades ag ON fs.GradeID = ag.Id
        INNER JOIN FeeTypes ft ON fs.FeeTypeID = ft.VID
        WHERE fs.VID = @VID;
    END
END
GO

-- 3. Save Stored Procedure
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'SpSave_FeeStructure')
    DROP PROCEDURE SpSave_FeeStructure;
GO

CREATE PROCEDURE SpSave_FeeStructure
    @VID INT,
    @AcademicSessionID INT,
    @ClassID INT,
    @GradeID INT,
    @FeeTypeID INT,
    @Amount DECIMAL(18,2),
    @IsActive BIT,
    @UserID INT,
    @UserIP NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    -- Check for duplicate combination (excluding current record on update)
    IF EXISTS (
        SELECT 1 FROM FeeStructures
        WHERE AcademicSessionID = @AcademicSessionID
          AND ClassID = @ClassID
          AND GradeID = @GradeID
          AND FeeTypeID = @FeeTypeID
          AND VID != @VID
    )
    BEGIN
        SELECT CAST(0 AS DECIMAL) AS VID, -1 AS ReturnCode, 'A fee structure for this combination already exists.' AS ReturnMessage;
        RETURN;
    END

    IF @VID = 0
    BEGIN
        -- INSERT
        INSERT INTO FeeStructures (AcademicSessionID, ClassID, GradeID, FeeTypeID, Amount, IsActive, InsertedBy, InsertedDate, InsertedIp)
        VALUES (@AcademicSessionID, @ClassID, @GradeID, @FeeTypeID, @Amount, @IsActive, @UserID, GETUTCDATE(), @UserIP);

        SELECT CAST(SCOPE_IDENTITY() AS DECIMAL) AS VID, 0 AS ReturnCode, 'Fee structure saved successfully.' AS ReturnMessage;
    END
    ELSE
    BEGIN
        -- UPDATE
        IF NOT EXISTS (SELECT 1 FROM FeeStructures WHERE VID = @VID)
        BEGIN
            SELECT CAST(0 AS DECIMAL) AS VID, -1 AS ReturnCode, 'Fee structure not found.' AS ReturnMessage;
            RETURN;
        END

        UPDATE FeeStructures
        SET AcademicSessionID = @AcademicSessionID,
            ClassID = @ClassID,
            GradeID = @GradeID,
            FeeTypeID = @FeeTypeID,
            Amount = @Amount,
            IsActive = @IsActive,
            UpdatedBy = @UserID,
            UpdatedDate = GETUTCDATE(),
            UpdatedIp = @UserIP
        WHERE VID = @VID;

        SELECT CAST(@VID AS DECIMAL) AS VID, 0 AS ReturnCode, 'Fee structure updated successfully.' AS ReturnMessage;
    END
END
GO

-- 4. Delete Stored Procedure
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'SpDelete_FeeStructure')
    DROP PROCEDURE SpDelete_FeeStructure;
GO

CREATE PROCEDURE SpDelete_FeeStructure
    @VID INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM FeeStructures WHERE VID = @VID)
    BEGIN
        SELECT CAST(0 AS DECIMAL) AS VID, -1 AS ReturnCode, 'Fee structure not found.' AS ReturnMessage;
        RETURN;
    END

    DELETE FROM FeeStructures WHERE VID = @VID;

    SELECT CAST(@VID AS DECIMAL) AS VID, 0 AS ReturnCode, 'Fee structure deleted successfully.' AS ReturnMessage;
END
GO

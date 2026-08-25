/* ============================================================
   SMSTeacher � stored procedures
   NOTE: ResponseDto.VID is a C# decimal, so every SP that returns
         a ResponseDto row MUST cast VID to DECIMAL. Returning a
         plain INT makes EF throw
         "Unable to cast object of type 'System.Int32' to type 'System.Decimal'"
         AFTER the insert/update/delete has already committed.
   ============================================================ */

IF OBJECT_ID('dbo.SpGet_SMSTeacher', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SpGet_SMSTeacher;
GO
CREATE PROCEDURE dbo.SpGet_SMSTeacher
    @VID INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        VID,
        VName,
        FatherName,
        Gender,
        CNIC,
        Mobile,
        Email,
        Address,
        Qualification,
        ExperienceYears,
        IsActive,
        InsertedBy, InsertedDate, InsertedIp,
        UpdatedBy, UpdatedDate, UpdatedIp
    FROM SMSTeacher
    WHERE (@VID = 0 OR VID = @VID);
END
GO

IF OBJECT_ID('dbo.SpSave_SMSTeacher', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SpSave_SMSTeacher;
GO
CREATE PROCEDURE dbo.SpSave_SMSTeacher
    @VID             INT = 0,
    @VName           VARCHAR(200),
    @FatherName      VARCHAR(200),
    @Gender          INT,
    @CNIC            VARCHAR(20),
    @Mobile          VARCHAR(20),
    @Email           VARCHAR(200),
    @Address         VARCHAR(500),
    @Qualification   VARCHAR(500),
    @ExperienceYears INT = NULL,
    @IsActive        BIT = 1,
    @UserID          INT,
    @UserIP          VARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    IF (@VID = 0)
    BEGIN
        INSERT INTO SMSTeacher
        (
            VName, FatherName, Gender, CNIC, Mobile, Email, Address,
            Qualification, ExperienceYears, IsActive,
            InsertedBy, InsertedDate, InsertedIp
        )
        VALUES
        (
            @VName, @FatherName, @Gender, @CNIC, @Mobile, @Email, @Address,
            @Qualification, @ExperienceYears, @IsActive,
            @UserID, GETDATE(), @UserIP
        );

        SELECT
            CAST(SCOPE_IDENTITY() AS DECIMAL(18,0)) AS VID,
            200 AS ReturnCode,
            'Record inserted successfully.' AS ReturnMessage;
    END
    ELSE
    BEGIN
        UPDATE SMSTeacher
        SET
            VName           = @VName,
            FatherName      = @FatherName,
            Gender          = @Gender,
            CNIC            = @CNIC,
            Mobile          = @Mobile,
            Email           = @Email,
            Address         = @Address,
            Qualification   = @Qualification,
            ExperienceYears = @ExperienceYears,
            IsActive        = @IsActive,
            UpdatedBy       = @UserID,
            UpdatedDate     = GETDATE(),
            UpdatedIp       = @UserIP
        WHERE VID = @VID;

        SELECT
            CAST(@VID AS DECIMAL(18,0)) AS VID,
            200 AS ReturnCode,
            'Record updated successfully.' AS ReturnMessage;
    END
END
GO

IF OBJECT_ID('dbo.SpDelete_SMSTeacher', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SpDelete_SMSTeacher;
GO
CREATE PROCEDURE dbo.SpDelete_SMSTeacher
    @VID INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM SMSTeacher WHERE VID = @VID;

    SELECT
        CAST(@VID AS DECIMAL(18,0)) AS VID,
        200 AS ReturnCode,
        'Record deleted successfully.' AS ReturnMessage;
END
GO

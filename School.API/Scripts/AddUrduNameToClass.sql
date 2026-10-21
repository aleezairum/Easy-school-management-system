/* ============================================================================
   SMSClass.VName_Urdu — the class name as it is written in Urdu
   ============================================================================

   The parents' documents are Urdu: the School Timing notice says
   "نہم جونیئر تا دہم", not "9 Junior to 10". Until now that wording lived in a
   lookup inside the notice page, which meant a class the school renamed, or a
   new class, silently printed its English name on a sheet going home to
   parents. The name belongs with the class.

   OPTIONAL. Existing classes keep NULL, and anything printing Urdu falls back
   to the built-in wording and then to the English name — nothing breaks while
   the office fills these in, and a class nobody translates is visibly English
   rather than blank.

   NVARCHAR, not VARCHAR. VARCHAR stores Urdu as a row of question marks, and
   the damage is silent and permanent — the save procedure below takes an
   NVARCHAR parameter for the same reason. (Note the existing @VName parameter
   is VARCHAR: that one is the English name and is deliberately left alone.)

   Idempotent: safe to run more than once.

   Run with:
     cd School.API\Scripts
     sqlcmd -S . -U sa -P <pwd> -d EasySchoolDB -i AddUrduNameToClass.sql
   ============================================================================ */

-- 1. Column -------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('SMSClass') AND name = 'VName_Urdu')
BEGIN
    ALTER TABLE SMSClass ADD VName_Urdu NVARCHAR(200) NULL;
    PRINT 'SMSClass.VName_Urdu added.';
END
ELSE
    PRINT 'SMSClass.VName_Urdu already present.';
GO

-- 2. Get ----------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'SpGet_SMSClass')
    DROP PROCEDURE SpGet_SMSClass;
GO

CREATE PROCEDURE SpGet_SMSClass
    @VID INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        VID,
        VName,
        VName_Urdu,
        IsActive,
        InsertedBy, InsertedDate, InsertedIp,
        UpdatedBy, UpdatedDate, UpdatedIp
    FROM SMSClass
    WHERE @VID = 0 OR VID = @VID;
END
GO

-- 3. Save ---------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'SpSave_SMSClass')
    DROP PROCEDURE SpSave_SMSClass;
GO

CREATE PROCEDURE [dbo].[SpSave_SMSClass]
    @VID INT = 0,
    @VName VARCHAR(200),
    -- NVARCHAR: a VARCHAR parameter would turn the Urdu into question marks
    -- before it ever reached the column.
    @VName_Urdu NVARCHAR(200) = NULL,
    @IsActive BIT = 1,
    @UserID INT,
    @UserIP VARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    -- An empty box means "no Urdu name", not an empty string, so the fallback
    -- in the printed notice can test for NULL and behave the same either way.
    DECLARE @Urdu NVARCHAR(200) = NULLIF(LTRIM(RTRIM(@VName_Urdu)), N'');

    IF (@VID = 0)
    BEGIN
        INSERT INTO SMSClass
        (
            VName, VName_Urdu, IsActive,
            InsertedBy, InsertedDate, InsertedIp
        )
        VALUES
        (
            @VName, @Urdu, @IsActive,
            @UserID, GETDATE(), @UserIP
        );

        SELECT
            SCOPE_IDENTITY() AS VID,
            200 AS ReturnCode,
            'Record inserted successfully.' AS ReturnMessage;
    END
    ELSE
    BEGIN
        UPDATE SMSClass
        SET
            VName = @VName,
            VName_Urdu = @Urdu,
            IsActive = @IsActive,
            UpdatedBy = @UserID,
            UpdatedDate = GETDATE()
            -- ,UpdatedIp = @UserIP
        WHERE VID = @VID;

        SELECT
            CAST(@VID AS NUMERIC(18,0)) AS VID,
            200 AS ReturnCode,
            'Record updated successfully.' AS ReturnMessage;
    END
END
GO

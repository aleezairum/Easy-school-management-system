-- ============================================================================
-- SMSClassTiming — class-wise school timing for STUDENTS
-- ============================================================================
--
-- HrSchoolTiming already exists and is deliberately left alone: that one is the
-- STAFF calendar. It is expanded into HrSchoolTimingDay, one row per working
-- day, and employee attendance and payroll read it to decide who was late. It
-- answers "when must the teacher be here?".
--
-- This table answers a different question — "when does MY CHILD come and go?" —
-- and the answer is not the same for everyone: Montessori usually leaves hours
-- before Class 10, and the two campuses run on their own clocks. So the timing
-- is held per (Campus, Class), which is exactly how a parent thinks about it.
--
-- There is one row per class, not a date range. A class opens and closes at the
-- same time every day until the school decides otherwise, and when it does, the
-- office edits the row. Modelling seasons here would mean a parent could be
-- told last winter's timing simply because nobody entered the new range.
--
-- Run with:
--   cd School.API\Scripts
--   sqlcmd -S . -d EasySchoolDB -U sa -P <pwd> -i SMSClassTiming.sql
-- ============================================================================

-- 1. Table -------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SMSClassTiming')
BEGIN
    CREATE TABLE SMSClassTiming (
        VID            INT IDENTITY(1,1) PRIMARY KEY,
        CampusID       INT      NOT NULL,
        ClassID        INT      NOT NULL,
        TimeIn         TIME(0)  NOT NULL,
        TimeOut        TIME(0)  NOT NULL,
        -- Fridays run short almost everywhere here. Both NULL means Friday is
        -- no different, and the reply then simply does not mention it.
        FridayTimeIn   TIME(0)  NULL,
        FridayTimeOut  TIME(0)  NULL,
        -- The afternoon academy, which most classes attend and some do not.
        -- Both NULL means this class has no academy session; the printed
        -- notice then leaves the class out of the academy table entirely
        -- rather than printing a blank row. No Friday pair: the academy runs
        -- Monday to Saturday on one clock.
        AcademyTimeIn  TIME(0)  NULL,
        AcademyTimeOut TIME(0)  NULL,
        IsActive       BIT      NOT NULL DEFAULT 1,
        InsertedBy     INT      NULL,
        InsertedDate   DATETIME NOT NULL DEFAULT GETDATE(),
        InsertedIp     NVARCHAR(50) NULL,
        UpdatedBy      INT      NULL,
        UpdatedDate    DATETIME NULL,
        UpdatedIp      NVARCHAR(50) NULL,
        -- One timing per class per campus. The save is an upsert on this pair,
        -- so a second row can never appear and leave the bot picking one at random.
        CONSTRAINT UQ_SMSClassTiming_Campus_Class UNIQUE (CampusID, ClassID)
    );
END
GO

-- 1b. Academy columns, for a table created before they existed ----------------
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SMSClassTiming')
   AND NOT EXISTS (SELECT 1 FROM sys.columns
                   WHERE object_id = OBJECT_ID('SMSClassTiming') AND name = 'AcademyTimeIn')
BEGIN
    ALTER TABLE SMSClassTiming ADD AcademyTimeIn TIME(0) NULL, AcademyTimeOut TIME(0) NULL;
    PRINT 'SMSClassTiming: academy columns added.';
END
GO

-- 2. Get ---------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'SpGet_SMSClassTiming')
    DROP PROCEDURE SpGet_SMSClassTiming;
GO

CREATE PROCEDURE SpGet_SMSClassTiming
    @VID      INT = 0,
    @CampusID INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    -- Times go out as HH:mm strings rather than TIME. The screen puts them
    -- straight into <input type="time">, which speaks exactly that, and it
    -- keeps a timezone-free value timezone-free all the way to the browser.
    IF @VID > 0
    BEGIN
        SELECT T.VID,
               T.CampusID,
               CP.VName                              AS CampusName,
               T.ClassID,
               LTRIM(RTRIM(C.VName))                 AS ClassName,
               C.VName_Urdu                          AS ClassNameUrdu,
               CONVERT(VARCHAR(5), T.TimeIn,  108)   AS TimeIn,
               CONVERT(VARCHAR(5), T.TimeOut, 108)   AS TimeOut,
               CONVERT(VARCHAR(5), T.FridayTimeIn,  108) AS FridayTimeIn,
               CONVERT(VARCHAR(5), T.FridayTimeOut, 108) AS FridayTimeOut,
               CONVERT(VARCHAR(5), T.AcademyTimeIn,  108) AS AcademyTimeIn,
               CONVERT(VARCHAR(5), T.AcademyTimeOut, 108) AS AcademyTimeOut,
               T.IsActive
        FROM SMSClassTiming T
        JOIN SMSClass   C  ON C.VID  = T.ClassID
        LEFT JOIN SMSCampus CP ON CP.VID = T.CampusID
        WHERE T.VID = @VID;
        RETURN;
    END

    IF @CampusID > 0
    BEGIN
        -- The editing grid: EVERY active class for this campus, whether or not
        -- a timing has been entered yet. A LEFT JOIN rather than a list of
        -- saved rows, because the screen exists to fill in the blanks — showing
        -- only what is already saved would hide the classes that still need doing.
        SELECT ISNULL(T.VID, 0)                      AS VID,
               @CampusID                             AS CampusID,
               CP.VName                              AS CampusName,
               C.VID                                 AS ClassID,
               LTRIM(RTRIM(C.VName))                 AS ClassName,
               C.VName_Urdu                          AS ClassNameUrdu,
               CONVERT(VARCHAR(5), T.TimeIn,  108)   AS TimeIn,
               CONVERT(VARCHAR(5), T.TimeOut, 108)   AS TimeOut,
               CONVERT(VARCHAR(5), T.FridayTimeIn,  108) AS FridayTimeIn,
               CONVERT(VARCHAR(5), T.FridayTimeOut, 108) AS FridayTimeOut,
               CONVERT(VARCHAR(5), T.AcademyTimeIn,  108) AS AcademyTimeIn,
               CONVERT(VARCHAR(5), T.AcademyTimeOut, 108) AS AcademyTimeOut,
               CAST(ISNULL(T.IsActive, 1) AS BIT)    AS IsActive
        FROM SMSClass C
        LEFT JOIN SMSClassTiming T ON T.ClassID = C.VID AND T.CampusID = @CampusID
        LEFT JOIN SMSCampus     CP ON CP.VID    = @CampusID
        WHERE ISNULL(C.IsActive, 1) = 1
        -- SMSClass.VID is the school's own progression (Montessori, Play Group,
        -- Nursery ... 10). The names cannot be sorted: half of them are words.
        ORDER BY C.VID;
        RETURN;
    END

    -- Everything saved, across campuses — the plain list.
    SELECT T.VID,
           T.CampusID,
           CP.VName                              AS CampusName,
           T.ClassID,
           LTRIM(RTRIM(C.VName))                 AS ClassName,
           C.VName_Urdu                          AS ClassNameUrdu,
           CONVERT(VARCHAR(5), T.TimeIn,  108)   AS TimeIn,
           CONVERT(VARCHAR(5), T.TimeOut, 108)   AS TimeOut,
           CONVERT(VARCHAR(5), T.FridayTimeIn,  108) AS FridayTimeIn,
           CONVERT(VARCHAR(5), T.FridayTimeOut, 108) AS FridayTimeOut,
           CONVERT(VARCHAR(5), T.AcademyTimeIn,  108) AS AcademyTimeIn,
           CONVERT(VARCHAR(5), T.AcademyTimeOut, 108) AS AcademyTimeOut,
           T.IsActive
    FROM SMSClassTiming T
    JOIN SMSClass   C  ON C.VID  = T.ClassID
    LEFT JOIN SMSCampus CP ON CP.VID = T.CampusID
    ORDER BY T.CampusID, C.VID;
END
GO

-- 3. Save --------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'SpSave_SMSClassTiming')
    DROP PROCEDURE SpSave_SMSClassTiming;
GO

CREATE PROCEDURE SpSave_SMSClassTiming
    @VID           INT,
    @CampusID      INT,
    @ClassID       INT,
    @TimeIn        VARCHAR(8),
    @TimeOut       VARCHAR(8),
    @FridayTimeIn  VARCHAR(8) = NULL,
    @FridayTimeOut VARCHAR(8) = NULL,
    @AcademyTimeIn  VARCHAR(8) = NULL,
    @AcademyTimeOut VARCHAR(8) = NULL,
    @IsActive      BIT = 1,
    @UserID        INT = 0,
    @UserIP        NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @In  TIME(0) = TRY_CAST(NULLIF(LTRIM(RTRIM(@TimeIn)),  '') AS TIME(0));
    DECLARE @Out TIME(0) = TRY_CAST(NULLIF(LTRIM(RTRIM(@TimeOut)), '') AS TIME(0));
    DECLARE @FIn  TIME(0) = TRY_CAST(NULLIF(LTRIM(RTRIM(@FridayTimeIn)),  '') AS TIME(0));
    DECLARE @FOut TIME(0) = TRY_CAST(NULLIF(LTRIM(RTRIM(@FridayTimeOut)), '') AS TIME(0));
    DECLARE @AIn  TIME(0) = TRY_CAST(NULLIF(LTRIM(RTRIM(@AcademyTimeIn)),  '') AS TIME(0));
    DECLARE @AOut TIME(0) = TRY_CAST(NULLIF(LTRIM(RTRIM(@AcademyTimeOut)), '') AS TIME(0));

    IF @In IS NULL OR @Out IS NULL
    BEGIN
        SELECT CAST(0 AS DECIMAL) AS VID, -1 AS ReturnCode,
               'Opening and closing time are both required.' AS ReturnMessage;
        RETURN;
    END

    IF @Out <= @In
    BEGIN
        SELECT CAST(0 AS DECIMAL) AS VID, -1 AS ReturnCode,
               'Closing time must be later than opening time.' AS ReturnMessage;
        RETURN;
    END

    -- Friday is all-or-nothing: one time on its own cannot describe a day, and
    -- half a Friday in the table would print as a broken line to a parent.
    IF (CASE WHEN @FIn  IS NULL THEN 1 ELSE 0 END)
     <> (CASE WHEN @FOut IS NULL THEN 1 ELSE 0 END)
    BEGIN
        SELECT CAST(0 AS DECIMAL) AS VID, -1 AS ReturnCode,
               'Enter both Friday times, or leave both blank.' AS ReturnMessage;
        RETURN;
    END

    IF @FIn IS NOT NULL AND @FOut <= @FIn
    BEGIN
        SELECT CAST(0 AS DECIMAL) AS VID, -1 AS ReturnCode,
               'Friday closing time must be later than Friday opening time.' AS ReturnMessage;
        RETURN;
    END

    -- Academy is all-or-nothing for the same reason as Friday, and it is
    -- optional: a class with neither time simply has no academy session.
    IF (CASE WHEN @AIn  IS NULL THEN 1 ELSE 0 END)
     <> (CASE WHEN @AOut IS NULL THEN 1 ELSE 0 END)
    BEGIN
        SELECT CAST(0 AS DECIMAL) AS VID, -1 AS ReturnCode,
               'Enter both academy times, or leave both blank.' AS ReturnMessage;
        RETURN;
    END

    IF @AIn IS NOT NULL AND @AOut <= @AIn
    BEGIN
        SELECT CAST(0 AS DECIMAL) AS VID, -1 AS ReturnCode,
               'Academy closing time must be later than academy opening time.' AS ReturnMessage;
        RETURN;
    END

    -- Upsert on (CampusID, ClassID) rather than trusting @VID. The grid sends
    -- 0 for a class that has never been saved, and two people saving the same
    -- screen at once would otherwise both insert and hit the unique constraint.
    DECLARE @Existing INT = (
        SELECT TOP 1 VID FROM SMSClassTiming
        WHERE CampusID = @CampusID AND ClassID = @ClassID);

    IF @Existing IS NULL
    BEGIN
        INSERT INTO SMSClassTiming
            (CampusID, ClassID, TimeIn, TimeOut, FridayTimeIn, FridayTimeOut,
             AcademyTimeIn, AcademyTimeOut,
             IsActive, InsertedBy, InsertedDate, InsertedIp)
        VALUES
            (@CampusID, @ClassID, @In, @Out, @FIn, @FOut,
             @AIn, @AOut,
             @IsActive, @UserID, GETDATE(), @UserIP);

        SELECT CAST(SCOPE_IDENTITY() AS DECIMAL) AS VID, 0 AS ReturnCode,
               'Class timing saved successfully.' AS ReturnMessage;
        RETURN;
    END

    UPDATE SMSClassTiming
    SET TimeIn        = @In,
        TimeOut       = @Out,
        FridayTimeIn  = @FIn,
        FridayTimeOut = @FOut,
        AcademyTimeIn  = @AIn,
        AcademyTimeOut = @AOut,
        IsActive      = @IsActive,
        UpdatedBy     = @UserID,
        UpdatedDate   = GETDATE(),
        UpdatedIp     = @UserIP
    WHERE VID = @Existing;

    SELECT CAST(@Existing AS DECIMAL) AS VID, 0 AS ReturnCode,
           'Class timing updated successfully.' AS ReturnMessage;
END
GO

-- 4. Delete ------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'SpDelete_SMSClassTiming')
    DROP PROCEDURE SpDelete_SMSClassTiming;
GO

CREATE PROCEDURE SpDelete_SMSClassTiming
    @VID INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM SMSClassTiming WHERE VID = @VID)
    BEGIN
        SELECT CAST(0 AS DECIMAL) AS VID, -1 AS ReturnCode,
               'Class timing not found.' AS ReturnMessage;
        RETURN;
    END

    DELETE FROM SMSClassTiming WHERE VID = @VID;

    SELECT CAST(@VID AS DECIMAL) AS VID, 0 AS ReturnCode,
           'Class timing deleted successfully.' AS ReturnMessage;
END
GO

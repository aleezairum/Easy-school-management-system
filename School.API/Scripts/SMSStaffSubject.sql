USE [EasySchoolDB]
GO
/* ============================================================================
   Staff Subjects  (assign Subjects to a Staff member / Teacher per Class+Section)
   ----------------------------------------------------------------------------
   Table   : SMSStaffSubject
   SPs     : SpGet_SMSStaffSubject          (list, optionally by VID / TeacherID)
             SpSave_SMSStaffSubject         (insert / update one row)
             SpDelete_SMSStaffSubject       (delete one row by VID)
             SpDeleteByTeacher_SMSStaffSubject (clear all rows of a teacher)

   The screen saves the WHOLE set for a teacher at once: the service clears the
   teacher's existing rows (SpDeleteByTeacher) then inserts the current set
   (SpSave per row) — same loop pattern used by SMSExamResult bulk save.

   Target: SQL Server 2019
   ============================================================================ */

/* ---- Table ---- */
IF OBJECT_ID('dbo.SMSStaffSubject', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SMSStaffSubject
    (
        VID          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        TeacherID    INT NOT NULL,
        ClassID      INT NOT NULL,
        SectionID    INT NOT NULL,
        SubjectID    INT NOT NULL,
        LectureNo    INT NULL,
        IsActive     BIT NOT NULL DEFAULT (1),
        InsertedBy   INT NULL,
        InsertedDate DATETIME NULL,
        InsertedIp   VARCHAR(200) NULL,
        UpdatedBy    INT NULL,
        UpdatedDate  DATETIME NULL,
        UpdatedIp    VARCHAR(200) NULL
    );
END
GO

/* ---- SpGet_SMSStaffSubject ---- */
IF OBJECT_ID('dbo.SpGet_SMSStaffSubject', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SpGet_SMSStaffSubject;
GO
CREATE PROCEDURE dbo.SpGet_SMSStaffSubject
    @VID       INT = 0,
    @TeacherID INT = 0,
    @ClassID   INT = 0,
    @SectionID INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        SS.VID        AS Vid,
        SS.TeacherID  AS TeacherId,
        T.VName       AS TeacherName,
        SS.ClassID    AS ClassId,
        C.VName       AS ClassName,
        SS.SectionID  AS SectionId,
        SE.VName      AS SectionName,
        SS.SubjectID  AS SubjectId,
        SU.VName      AS SubjectName,
        SS.LectureNo  AS LectureNo,
        SS.IsActive   AS IsActive
    FROM        dbo.SMSStaffSubject SS
    LEFT JOIN   dbo.SMSTeacher T  ON T.VID  = SS.TeacherID
    LEFT JOIN   dbo.SMSClass   C  ON C.VID  = SS.ClassID
    LEFT JOIN   dbo.SMSSection SE ON SE.VID = SS.SectionID
    LEFT JOIN   dbo.SMSSubject SU ON SU.VID = SS.SubjectID
    WHERE       (@VID = 0 OR SS.VID = @VID)
      AND       (@TeacherID = 0 OR SS.TeacherID = @TeacherID)
      AND       (@ClassID   = 0 OR SS.ClassID   = @ClassID)
      AND       (@SectionID = 0 OR SS.SectionID = @SectionID)
      AND       SS.IsActive = 1
    ORDER BY    T.VName, SS.ClassID, SS.SectionID, SS.LectureNo;
END
GO

/* ---- SpSave_SMSStaffSubject ---- */
IF OBJECT_ID('dbo.SpSave_SMSStaffSubject', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SpSave_SMSStaffSubject;
GO
CREATE PROCEDURE dbo.SpSave_SMSStaffSubject
    @VID       INT = 0,
    @TeacherID INT,
    @ClassID   INT,
    @SectionID INT,
    @SubjectID INT,
    @LectureNo INT = NULL,
    @IsActive  BIT = 1,
    @UserID    INT,
    @UserIP    VARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    IF (@VID = 0)
    BEGIN
        INSERT INTO dbo.SMSStaffSubject
        (
            TeacherID, ClassID, SectionID, SubjectID, LectureNo, IsActive,
            InsertedBy, InsertedDate, InsertedIp
        )
        VALUES
        (
            @TeacherID, @ClassID, @SectionID, @SubjectID, @LectureNo, @IsActive,
            @UserID, GETDATE(), @UserIP
        );

        SELECT
            CAST(SCOPE_IDENTITY() AS DECIMAL(18,0)) AS VID,
            200 AS ReturnCode,
            'Record inserted successfully.' AS ReturnMessage;
    END
    ELSE
    BEGIN
        UPDATE dbo.SMSStaffSubject
        SET
            TeacherID   = @TeacherID,
            ClassID     = @ClassID,
            SectionID   = @SectionID,
            SubjectID   = @SubjectID,
            LectureNo   = @LectureNo,
            IsActive    = @IsActive,
            UpdatedBy   = @UserID,
            UpdatedDate = GETDATE(),
            UpdatedIp   = @UserIP
        WHERE VID = @VID;

        SELECT
            CAST(@VID AS DECIMAL(18,0)) AS VID,
            200 AS ReturnCode,
            'Record updated successfully.' AS ReturnMessage;
    END
END
GO

/* ---- SpDelete_SMSStaffSubject ---- */
IF OBJECT_ID('dbo.SpDelete_SMSStaffSubject', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SpDelete_SMSStaffSubject;
GO
CREATE PROCEDURE dbo.SpDelete_SMSStaffSubject
    @VID INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.SMSStaffSubject WHERE VID = @VID;

    SELECT
        CAST(@VID AS DECIMAL(18,0)) AS VID,
        200 AS ReturnCode,
        'Record deleted successfully.' AS ReturnMessage;
END
GO

/* ---- SpDeleteByTeacher_SMSStaffSubject ---- */
IF OBJECT_ID('dbo.SpDeleteByTeacher_SMSStaffSubject', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SpDeleteByTeacher_SMSStaffSubject;
GO
CREATE PROCEDURE dbo.SpDeleteByTeacher_SMSStaffSubject
    @TeacherID INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.SMSStaffSubject WHERE TeacherID = @TeacherID;

    SELECT
        @TeacherID AS VID,
        200 AS ReturnCode,
        'Records cleared successfully.' AS ReturnMessage;
END
GO

USE [EasySchoolDB]
GO
/* ============================================================================
   Staff Subjects  (assign Subjects to a Staff member per Exam + Class + Section)
   ----------------------------------------------------------------------------
   Table   : SMSStaffSubject
   SPs     : SpGet_SMSStaffSubject          (list, optionally by VID / Teacher /
                                             Class / Section / Exam)
             SpSave_SMSStaffSubject         (insert / update one row)
             SpDelete_SMSStaffSubject       (delete one row by VID)
             SpDeleteByTeacher_SMSStaffSubject (clear a teacher, within one exam)
             SpCopy_SMSStaffSubject         (carry one exam's assignments into
                                             another - "copy from previous exam")

   Assignments are PER EXAM. A row says "for this exam, this staff member taught
   this subject in this class+section", which is what lets a teacher's subjects
   change between terms without rewriting the previous term's reports. The exam
   reports (SpRPT_SMSExamSubjectWise / TeacherWise / TeacherComparison) read only
   the assignments belonging to the exam they are reporting on - so a brand new
   exam shows no teacher names until its assignments are entered or copied.

   The screen saves the WHOLE set for a teacher at once: the service clears the
   teacher's existing rows (SpDeleteByTeacher) then inserts the current set
   (SpSave per row) - same loop pattern used by SMSExamResult bulk save.

   Staff come from dbo.Employees (SMSTeacher was renamed into it, keeping its
   Ids, so TeacherID values are unchanged - see RenameTeacherToEmployee.sql).

   On an existing database, run AddExamToStaffSubject.sql FIRST: it adds the
   ExamID column and backfills it. This script only creates the table when it is
   missing, so it will not add the column on its own.

   Target: SQL Server 2019
   ============================================================================ */

/* ---- Table ---- */
IF OBJECT_ID('dbo.SMSStaffSubject', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SMSStaffSubject
    (
        VID          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ExamID       INT NOT NULL,
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

    /* The two ways these rows are read: the screen filters by exam (+ teacher),
       the reports look them up by exam + class + section + subject. */
    CREATE INDEX IX_SMSStaffSubject_Exam_Teacher
        ON dbo.SMSStaffSubject (ExamID, TeacherID) INCLUDE (ClassID, SectionID, SubjectID, LectureNo, IsActive);

    CREATE INDEX IX_SMSStaffSubject_Exam_Lookup
        ON dbo.SMSStaffSubject (ExamID, ClassID, SectionID, SubjectID) INCLUDE (TeacherID, LectureNo, IsActive);
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
    @SectionID INT = 0,
    @ExamID    INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        SS.VID        AS Vid,
        SS.ExamID     AS ExamId,
        EX.VName      AS ExamName,
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
    LEFT JOIN   dbo.SMSExam    EX ON EX.VID = SS.ExamID
    LEFT JOIN   dbo.Employees  T  ON T.Id   = SS.TeacherID
    LEFT JOIN   dbo.SMSClass   C  ON C.VID  = SS.ClassID
    LEFT JOIN   dbo.SMSSection SE ON SE.VID = SS.SectionID
    LEFT JOIN   dbo.SMSSubject SU ON SU.VID = SS.SubjectID
    WHERE       (@VID = 0 OR SS.VID = @VID)
      AND       (@TeacherID = 0 OR SS.TeacherID = @TeacherID)
      AND       (@ClassID   = 0 OR SS.ClassID   = @ClassID)
      AND       (@SectionID = 0 OR SS.SectionID = @SectionID)
      AND       (@ExamID    = 0 OR SS.ExamID    = @ExamID)
      AND       SS.IsActive = 1
    ORDER BY    EX.VID DESC, T.VName, SS.ClassID, SS.SectionID, SS.LectureNo;
END
GO

/* ---- SpSave_SMSStaffSubject ---- */
IF OBJECT_ID('dbo.SpSave_SMSStaffSubject', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SpSave_SMSStaffSubject;
GO
CREATE PROCEDURE dbo.SpSave_SMSStaffSubject
    @VID       INT = 0,
    @ExamID    INT,
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

    IF NOT EXISTS (SELECT 1 FROM dbo.SMSExam WHERE VID = @ExamID)
    BEGIN
        SELECT CAST(0 AS DECIMAL(18,0)) AS VID,
               400 AS ReturnCode,
               'Please select a valid exam.' AS ReturnMessage;
        RETURN;
    END

    /* The same subject may legitimately repeat under a different LectureNo, so
       "already assigned" means the whole tuple matches, lecture number included. */
    IF EXISTS (SELECT 1 FROM dbo.SMSStaffSubject
               WHERE ExamID    = @ExamID
                 AND TeacherID = @TeacherID
                 AND ClassID   = @ClassID
                 AND SectionID = @SectionID
                 AND SubjectID = @SubjectID
                 AND ISNULL(LectureNo, -1) = ISNULL(@LectureNo, -1)
                 AND VID <> @VID)
    BEGIN
        SELECT CAST(0 AS DECIMAL(18,0)) AS VID,
               400 AS ReturnCode,
               'This subject is already assigned to that staff member for this exam.' AS ReturnMessage;
        RETURN;
    END

    IF (@VID = 0)
    BEGIN
        INSERT INTO dbo.SMSStaffSubject
        (
            ExamID, TeacherID, ClassID, SectionID, SubjectID, LectureNo, IsActive,
            InsertedBy, InsertedDate, InsertedIp
        )
        VALUES
        (
            @ExamID, @TeacherID, @ClassID, @SectionID, @SubjectID, @LectureNo, @IsActive,
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
            ExamID      = @ExamID,
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

/* ---- SpDeleteByTeacher_SMSStaffSubject ----
   @ExamID = 0 clears the teacher in EVERY exam, which is almost never what you
   want now that assignments are per-exam - the screen always passes an exam. */
IF OBJECT_ID('dbo.SpDeleteByTeacher_SMSStaffSubject', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SpDeleteByTeacher_SMSStaffSubject;
GO
CREATE PROCEDURE dbo.SpDeleteByTeacher_SMSStaffSubject
    @TeacherID INT,
    @ExamID    INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.SMSStaffSubject
    WHERE TeacherID = @TeacherID
      AND (@ExamID = 0 OR ExamID = @ExamID);

    SELECT
        CAST(@@ROWCOUNT AS DECIMAL(18,0)) AS VID,
        200 AS ReturnCode,
        'Records cleared successfully.' AS ReturnMessage;
END
GO

/* ---- SpCopy_SMSStaffSubject ----
   Carry a whole exam's assignments into another one. Skips anything the target
   exam already has (same teacher + class + section + subject + lecture no), so
   it is safe to run twice and safe to run after hand-entering a few rows. */
IF OBJECT_ID('dbo.SpCopy_SMSStaffSubject', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SpCopy_SMSStaffSubject;
GO
CREATE PROCEDURE dbo.SpCopy_SMSStaffSubject
    @FromExamID INT,
    @ToExamID   INT,
    @UserID     INT,
    @UserIP     VARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromExamID = @ToExamID
    BEGIN
        SELECT CAST(0 AS DECIMAL(18,0)) AS VID, 400 AS ReturnCode,
               'Source and target exam cannot be the same.' AS ReturnMessage;
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.SMSExam WHERE VID = @FromExamID)
    BEGIN
        SELECT CAST(0 AS DECIMAL(18,0)) AS VID, 400 AS ReturnCode,
               'The exam to copy from does not exist.' AS ReturnMessage;
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.SMSExam WHERE VID = @ToExamID)
    BEGIN
        SELECT CAST(0 AS DECIMAL(18,0)) AS VID, 400 AS ReturnCode,
               'The exam to copy into does not exist.' AS ReturnMessage;
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.SMSStaffSubject WHERE ExamID = @FromExamID AND IsActive = 1)
    BEGIN
        SELECT CAST(0 AS DECIMAL(18,0)) AS VID, 400 AS ReturnCode,
               'That exam has no subject assignments to copy.' AS ReturnMessage;
        RETURN;
    END

    INSERT INTO dbo.SMSStaffSubject
    (
        ExamID, TeacherID, ClassID, SectionID, SubjectID, LectureNo, IsActive,
        InsertedBy, InsertedDate, InsertedIp
    )
    SELECT
        @ToExamID, s.TeacherID, s.ClassID, s.SectionID, s.SubjectID, s.LectureNo, 1,
        @UserID, GETDATE(), @UserIP
    FROM   dbo.SMSStaffSubject s
    WHERE  s.ExamID   = @FromExamID
      AND  s.IsActive = 1
      AND  NOT EXISTS (SELECT 1 FROM dbo.SMSStaffSubject d
                       WHERE d.ExamID    = @ToExamID
                         AND d.TeacherID = s.TeacherID
                         AND d.ClassID   = s.ClassID
                         AND d.SectionID = s.SectionID
                         AND d.SubjectID = s.SubjectID
                         AND ISNULL(d.LectureNo, -1) = ISNULL(s.LectureNo, -1));

    DECLARE @Copied INT = @@ROWCOUNT;

    SELECT
        CAST(@Copied AS DECIMAL(18,0)) AS VID,
        200 AS ReturnCode,
        CASE WHEN @Copied = 0
             THEN 'Nothing to copy - the target exam already has these assignments.'
             ELSE CAST(@Copied AS VARCHAR(10)) + ' assignment(s) copied.'
        END AS ReturnMessage;
END
GO

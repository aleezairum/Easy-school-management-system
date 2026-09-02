/* ============================================================================
   SMSStaffSubject gets an ExamID.

   Subject assignments become per-exam: who taught which subject in which
   class+section is now recorded against a specific exam, so a teacher's
   subjects can change between terms without rewriting the previous term's
   reports.

   Existing rows are all backfilled to one exam - the oldest active one - which
   is exactly right here, because they were entered before exams were a
   distinction. From then on, "Copy from previous exam" is how a new term gets
   its assignments.

   >>> The exam reports read ONLY the assignments of the exam being reported on.
       A new exam with nothing copied into it shows no teacher names. <<<

   This script does the COLUMN. The stored procedures that use it live in
   SMSStaffSubject.sql - run that next.

   Run order:  RenameTeacherToEmployee.sql   (the staff join reads dbo.Employees)
               -> THIS SCRIPT
               -> SMSStaffSubject.sql
               -> SpRPT_SMSExamSubjectWise.sql
                  SpRPT_SMSExamTeacherWise.sql
                  SpRPT_SMSExamTeacherComparison.sql

   Idempotent: every batch is guarded on schema state, safe to re-run.

   Target: SQL Server 2019
   ============================================================================ */

USE [EasySchoolDB]
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ---------------------------------------------------------------------------
   1. Column (own batch, so the rest of the script can see it)
   --------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.SMSStaffSubject', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.SMSStaffSubject', 'ExamID') IS NULL
BEGIN
    ALTER TABLE dbo.SMSStaffSubject ADD ExamID INT NULL;
    PRINT '>> SMSStaffSubject.ExamID added.';
END
GO

/* ---------------------------------------------------------------------------
   2. Backfill.

   Every existing row predates the idea of per-exam assignments, so they all
   belong to the same exam: the oldest active one. With no exam at all the
   column stays nullable and the constraints are skipped - create an exam and
   re-run.
   --------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.SMSStaffSubject', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.SMSStaffSubject', 'ExamID') IS NOT NULL
BEGIN
    /* A DECLARE initialiser cannot hold a subquery, hence the two SELECTs. */
    DECLARE @DefaultExam INT;
    DECLARE @DefaultName NVARCHAR(150);
    DECLARE @Filled      INT;

    SELECT @DefaultExam = MIN(VID) FROM dbo.SMSExam WHERE IsActive = 1;
    IF @DefaultExam IS NULL
        SELECT @DefaultExam = MIN(VID) FROM dbo.SMSExam;

    IF @DefaultExam IS NULL
        PRINT '!! No rows in SMSExam - ExamID left nullable. Create an exam and re-run this script.';
    ELSE
    BEGIN
        SELECT @DefaultName = VName FROM dbo.SMSExam WHERE VID = @DefaultExam;

        UPDATE dbo.SMSStaffSubject SET ExamID = @DefaultExam WHERE ExamID IS NULL;
        SET @Filled = @@ROWCOUNT;

        PRINT '>> Existing assignments backfilled to exam ' + CAST(@DefaultExam AS VARCHAR(10))
            + ' (' + ISNULL(@DefaultName, '?') + '): '
            + CAST(@Filled AS VARCHAR(10)) + ' row(s).';
    END
END
GO

/* ---------------------------------------------------------------------------
   3. Lock it down: NOT NULL, foreign key, and the two lookup indexes.
   --------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.SMSStaffSubject', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.SMSStaffSubject', 'ExamID') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.SMSStaffSubject WHERE ExamID IS NULL)
   AND EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.SMSStaffSubject')
                 AND name = 'ExamID' AND is_nullable = 1)
BEGIN
    ALTER TABLE dbo.SMSStaffSubject ALTER COLUMN ExamID INT NOT NULL;
    PRINT '>> SMSStaffSubject.ExamID is now NOT NULL.';
END
GO

IF OBJECT_ID('dbo.SMSStaffSubject', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.SMSStaffSubject', 'ExamID') IS NOT NULL
BEGIN
    IF OBJECT_ID('dbo.SMSExam', 'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.SMSStaffSubject s
                       WHERE NOT EXISTS (SELECT 1 FROM dbo.SMSExam e WHERE e.VID = s.ExamID))
       AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SMSStaffSubject_SMSExam')
        ALTER TABLE dbo.SMSStaffSubject
            ADD CONSTRAINT FK_SMSStaffSubject_SMSExam FOREIGN KEY (ExamID) REFERENCES dbo.SMSExam(VID);

    /* The two ways these rows are read: the screen filters by exam (+ teacher),
       the reports look them up by exam + class + section + subject. */
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = 'IX_SMSStaffSubject_Exam_Teacher' AND object_id = OBJECT_ID('dbo.SMSStaffSubject'))
        CREATE INDEX IX_SMSStaffSubject_Exam_Teacher
            ON dbo.SMSStaffSubject (ExamID, TeacherID) INCLUDE (ClassID, SectionID, SubjectID, LectureNo, IsActive);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = 'IX_SMSStaffSubject_Exam_Lookup' AND object_id = OBJECT_ID('dbo.SMSStaffSubject'))
        CREATE INDEX IX_SMSStaffSubject_Exam_Lookup
            ON dbo.SMSStaffSubject (ExamID, ClassID, SectionID, SubjectID) INCLUDE (TeacherID, LectureNo, IsActive);

    PRINT '>> Foreign key and indexes in place.';
END
GO


/* ---------------------------------------------------------------------------
   Result
   --------------------------------------------------------------------------- */
SELECT  EX.VID AS ExamId, EX.VName AS ExamName, COUNT(SS.VID) AS Assignments
FROM    dbo.SMSExam EX
LEFT JOIN dbo.SMSStaffSubject SS ON SS.ExamID = EX.VID AND SS.IsActive = 1
GROUP BY EX.VID, EX.VName
ORDER BY EX.VID;
GO

PRINT '';
PRINT '=== NEXT ===';
PRINT '    SMSStaffSubject.sql                  (procedures, incl. SpCopy_SMSStaffSubject)';
PRINT '    SpRPT_SMSExamSubjectWise.sql         (report joins, now exam-scoped)';
PRINT '    SpRPT_SMSExamTeacherWise.sql';
PRINT '    SpRPT_SMSExamTeacherComparison.sql';
GO

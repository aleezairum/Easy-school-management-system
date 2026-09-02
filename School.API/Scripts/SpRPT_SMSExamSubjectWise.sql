USE [EasySchoolDB]
GO
/* ============================================================================
   SpRPT_SMSExamSubjectWise
   ----------------------------------------------------------------------------
   Subject Wise Result report for ONE Exam, SECTION WISE.

   The report is always produced per Section: one block of per-subject stats for
   every (Class, Section) in scope. The scope is a multi-select set of Classes
   and a multi-select set of Sections, passed as comma-separated id lists. An
   empty / NULL list means "no filter on that dimension" — so leaving both empty
   produces the report for ALL sections in the school.

   Returns 1 result set — one row per (Class, Section, Subject):
     ClassId, ClassName, SectionId, SectionName,
     SubjectId, SubjectName,
     TotalStudents, Appeared, Passed, Failed,
     PassPercentByPassMarks       (Passed   / Appeared            * 100),
     AggregateTotalMarks          (SUM of subject max marks, appeared only),
     TotalObtainedMarks           (SUM of obtained marks,    appeared only),
     PassPercentByObtainedMarks   (Obtained / AggregateTotal      * 100),
     Average                      (Obtained / Appeared),
     TeacherName                  (from SMSStaffSubject -> Employees, the
                                   teacher assigned to that Class/Section/Subject
                                   FOR THIS EXAM; assignments are per-exam, so it
                                   is NULL until Staff Subjects are entered or
                                   copied into @ExamID)

   A student "appears" when IsAbsent = 0; "passes" when IsAbsent = 0 and
   IsPassed = 1; "fails" when IsAbsent = 0 and IsPassed = 0 (absentees are
   neither passed nor failed — same rule as SpRPT_SMSExamExecutive).

   The "Position" (subject ranking by Average, per section) is computed in the
   application layer, so it is not returned here.

   Parameters:
     @ExamID     INT          - the exam                                (required)
     @ClassIDs   VARCHAR(MAX) - class ids   (comma separated, optional; empty = all)
     @SectionIDs VARCHAR(MAX) - section ids (comma separated, optional; empty = all)

   Target: SQL Server 2019 (DB compat level 120 — uses an XML splitter, not
           STRING_SPLIT, which needs compat level 130+).
   ============================================================================ */
IF OBJECT_ID('dbo.SpRPT_SMSExamSubjectWise', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SpRPT_SMSExamSubjectWise;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SpRPT_SMSExamSubjectWise]
(
    @ExamID     INT,
    @ClassIDs   VARCHAR(MAX) = NULL,
    @SectionIDs VARCHAR(MAX) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    -- ── Parse the comma-separated id lists into table variables ─────────────
    -- (empty / NULL => the table stays empty => that dimension is NOT filtered)
    DECLARE @Class TABLE (Id INT PRIMARY KEY);
    DECLARE @Sec   TABLE (Id INT PRIMARY KEY);

    -- XML-shred each "1,2,3" list into integer ids (compat-120 safe).
    DECLARE @xC XML = CAST('<i>' + REPLACE(ISNULL(@ClassIDs,'')  , ',', '</i><i>') + '</i>' AS XML);
    DECLARE @xS XML = CAST('<i>' + REPLACE(ISNULL(@SectionIDs,''), ',', '</i><i>') + '</i>' AS XML);

    -- NULLIF(...,'') keeps empty entries out: TRY_CAST('' AS INT) is 0 (not NULL),
    -- which would otherwise inject a bogus id 0 and break the "no filter" case.
    INSERT INTO @Class(Id)
        SELECT DISTINCT TRY_CAST(NULLIF(LTRIM(RTRIM(n.x.value('.', 'VARCHAR(20)'))), '') AS INT)
        FROM   @xC.nodes('/i') AS n(x)
        WHERE  TRY_CAST(NULLIF(LTRIM(RTRIM(n.x.value('.', 'VARCHAR(20)'))), '') AS INT) IS NOT NULL;
    INSERT INTO @Sec(Id)
        SELECT DISTINCT TRY_CAST(NULLIF(LTRIM(RTRIM(n.x.value('.', 'VARCHAR(20)'))), '') AS INT)
        FROM   @xS.nodes('/i') AS n(x)
        WHERE  TRY_CAST(NULLIF(LTRIM(RTRIM(n.x.value('.', 'VARCHAR(20)'))), '') AS INT) IS NOT NULL;

    DECLARE @hasClass BIT = CASE WHEN EXISTS (SELECT 1 FROM @Class) THEN 1 ELSE 0 END;
    DECLARE @hasSec   BIT = CASE WHEN EXISTS (SELECT 1 FROM @Sec)   THEN 1 ELSE 0 END;

    SELECT
        R.ClassID                          AS ClassId,
        ISNULL(CL.VName, 'Unknown')        AS ClassName,
        R.SectionID                        AS SectionId,
        ISNULL(SE.VName, 'Unknown')        AS SectionName,
        SU.VID                             AS SubjectId,
        SU.VName                           AS SubjectName,
        COUNT(*) AS TotalStudents,
        SUM(CASE WHEN R.IsAbsent = 0 THEN 1 ELSE 0 END) AS Appeared,
        SUM(CASE WHEN R.IsAbsent = 0 AND ISNULL(R.IsPassed, 0) = 1 THEN 1 ELSE 0 END) AS Passed,
        SUM(CASE WHEN R.IsAbsent = 0 AND ISNULL(R.IsPassed, 0) = 0 THEN 1 ELSE 0 END) AS Failed,
        CAST(CASE WHEN SUM(CASE WHEN R.IsAbsent = 0 THEN 1 ELSE 0 END) > 0
                  THEN SUM(CASE WHEN R.IsAbsent = 0 AND ISNULL(R.IsPassed, 0) = 1 THEN 1 ELSE 0 END) * 100.0
                       / SUM(CASE WHEN R.IsAbsent = 0 THEN 1 ELSE 0 END)
                  ELSE 0 END AS DECIMAL(5,2)) AS PassPercentByPassMarks,
        CAST(SUM(CASE WHEN R.IsAbsent = 0 THEN ISNULL(R.TotalMarks, 0)    ELSE 0 END) AS DECIMAL(9,2)) AS AggregateTotalMarks,
        CAST(SUM(CASE WHEN R.IsAbsent = 0 THEN ISNULL(R.MarksObtained, 0) ELSE 0 END) AS DECIMAL(9,2)) AS TotalObtainedMarks,
        CAST(CASE WHEN SUM(CASE WHEN R.IsAbsent = 0 THEN ISNULL(R.TotalMarks, 0) ELSE 0 END) > 0
                  THEN SUM(CASE WHEN R.IsAbsent = 0 THEN ISNULL(R.MarksObtained, 0) ELSE 0 END) * 100.0
                       / SUM(CASE WHEN R.IsAbsent = 0 THEN ISNULL(R.TotalMarks, 0) ELSE 0 END)
                  ELSE 0 END AS DECIMAL(5,2)) AS PassPercentByObtainedMarks,
        CAST(CASE WHEN SUM(CASE WHEN R.IsAbsent = 0 THEN 1 ELSE 0 END) > 0
                  THEN SUM(CASE WHEN R.IsAbsent = 0 THEN ISNULL(R.MarksObtained, 0) ELSE 0 END) * 1.0
                       / SUM(CASE WHEN R.IsAbsent = 0 THEN 1 ELSE 0 END)
                  ELSE 0 END AS DECIMAL(9,2)) AS Average,
        (SELECT TOP 1 T.VName
         FROM   SMSStaffSubject SS
         INNER JOIN Employees   T ON T.Id  = SS.TeacherID
         WHERE  SS.ExamID    = @ExamID
           AND  SS.ClassID   = R.ClassID
           AND  SS.SectionID = R.SectionID
           AND  SS.SubjectID = SU.VID
           AND  SS.IsActive  = 1
         ORDER BY SS.LectureNo) AS TeacherName
    FROM        SMSExamResult R
    INNER JOIN  SMSSubject    SU ON R.SubjectID = SU.VID
    LEFT  JOIN  SMSClass      CL ON CL.VID = R.ClassID
    LEFT  JOIN  SMSSection    SE ON SE.VID = R.SectionID
    WHERE       R.ExamID    = @ExamID
      AND       R.IsActive  = 1
      AND       (@hasClass = 0 OR R.ClassID   IN (SELECT Id FROM @Class))
      AND       (@hasSec   = 0 OR R.SectionID IN (SELECT Id FROM @Sec))
      AND       EXISTS (SELECT 1 FROM SMSStudentAcademic SA
                        WHERE SA.StudentID = R.StudentID AND SA.IsActive = 1 AND SA.StatusID = 1)
    GROUP BY    R.ClassID, CL.VName, R.SectionID, SE.VName, SU.VID, SU.VName
    ORDER BY    CL.VName, SE.VName, SU.VName;
END
GO

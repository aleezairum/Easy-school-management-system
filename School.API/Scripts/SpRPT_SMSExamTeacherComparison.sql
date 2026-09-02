USE [EasySchoolDB]
GO
/* ============================================================================
   SpRPT_SMSExamTeacherComparison
   ----------------------------------------------------------------------------
   Side-by-side TEACHER RESULT COMPARISON for ONE Exam.

   The report (see "Teacher Result Comparison Report") shows two independent
   teacher lists next to each other — a LEFT scope and a RIGHT scope — each
   defined by a multi-select set of Classes and a multi-select set of Sections
   (passed as comma-separated id lists).

   For every teacher inside a scope the "Result %" is the SAME average that the
   Teacher Wise report shows: the mean of that teacher's assigned subject
   percentages.  A subject percentage is the "Average%" of the Teacher Wise /
   Subject Wise reports, i.e. the mean of
       - Pass % by Pass Marks      (Passed   / Appeared   * 100)
       - Pass % by Obtained Marks  (Obtained / TotalMarks * 100)
   over the appeared students (IsAbsent = 0) of that Subject/Class/Section,
   driven by the active teacher -> subject assignments OF THIS EXAM
   (dbo.SMSStaffSubject is per-exam).
   Teachers are then ranked into Positions within their own scope (highest
   avg = 1; ties share a position).

   Returns ONE result set (both scopes stacked), one row per (Side, Teacher):
     Side          CHAR(1)  'L' = left scope, 'R' = right scope
     Sr            INT      running 1..n inside the scope (display order)
     TeacherId     INT
     TeacherName   NVARCHAR
     ResultPercent DECIMAL(5,2)
     Position      INT      DENSE_RANK within the scope (1 = best)

   Scope filtering (each dimension is independent; an empty/NULL list = no
   filter on that dimension):
        ClassID   IN (@xClassIDs)   AND   SectionID IN (@xSectionIDs)

   Parameters:
     @ExamID      INT          - the exam                                (required)
     @LClassIDs   VARCHAR(MAX) - left  scope class ids   (comma separated, optional)
     @LSectionIDs VARCHAR(MAX) - left  scope section ids (comma separated, optional)
     @RClassIDs   VARCHAR(MAX) - right scope class ids   (comma separated, optional)
     @RSectionIDs VARCHAR(MAX) - right scope section ids (comma separated, optional)

   Target: SQL Server 2019 (DB compat level 120 — uses an XML splitter, not
           STRING_SPLIT, which needs compat level 130+).
   ============================================================================ */
IF OBJECT_ID('dbo.SpRPT_SMSExamTeacherComparison', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SpRPT_SMSExamTeacherComparison;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SpRPT_SMSExamTeacherComparison]
(
    @ExamID      INT,
    @LClassIDs   VARCHAR(MAX) = NULL,
    @LSectionIDs VARCHAR(MAX) = NULL,
    @RClassIDs   VARCHAR(MAX) = NULL,
    @RSectionIDs VARCHAR(MAX) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    -- ── Parse the comma-separated id lists into table variables ─────────────
    -- (empty / NULL => the table stays empty => that dimension is NOT filtered)
    DECLARE @LClass TABLE (Id INT PRIMARY KEY);
    DECLARE @LSec   TABLE (Id INT PRIMARY KEY);
    DECLARE @RClass TABLE (Id INT PRIMARY KEY);
    DECLARE @RSec   TABLE (Id INT PRIMARY KEY);

    -- XML-shred each "1,2,3" list into integer ids (compat-120 safe).
    DECLARE @xL  XML = CAST('<i>' + REPLACE(ISNULL(@LClassIDs,'')  , ',', '</i><i>') + '</i>' AS XML);
    DECLARE @xLS XML = CAST('<i>' + REPLACE(ISNULL(@LSectionIDs,''), ',', '</i><i>') + '</i>' AS XML);
    DECLARE @xR  XML = CAST('<i>' + REPLACE(ISNULL(@RClassIDs,'')  , ',', '</i><i>') + '</i>' AS XML);
    DECLARE @xRS XML = CAST('<i>' + REPLACE(ISNULL(@RSectionIDs,''), ',', '</i><i>') + '</i>' AS XML);

    -- NULLIF(...,'') keeps empty entries out: TRY_CAST('' AS INT) is 0 (not NULL),
    -- which would otherwise inject a bogus id 0 and break the "no filter" case.
    INSERT INTO @LClass(Id)
        SELECT DISTINCT TRY_CAST(NULLIF(LTRIM(RTRIM(n.x.value('.', 'VARCHAR(20)'))), '') AS INT)
        FROM   @xL.nodes('/i') AS n(x)
        WHERE  TRY_CAST(NULLIF(LTRIM(RTRIM(n.x.value('.', 'VARCHAR(20)'))), '') AS INT) IS NOT NULL;
    INSERT INTO @LSec(Id)
        SELECT DISTINCT TRY_CAST(NULLIF(LTRIM(RTRIM(n.x.value('.', 'VARCHAR(20)'))), '') AS INT)
        FROM   @xLS.nodes('/i') AS n(x)
        WHERE  TRY_CAST(NULLIF(LTRIM(RTRIM(n.x.value('.', 'VARCHAR(20)'))), '') AS INT) IS NOT NULL;
    INSERT INTO @RClass(Id)
        SELECT DISTINCT TRY_CAST(NULLIF(LTRIM(RTRIM(n.x.value('.', 'VARCHAR(20)'))), '') AS INT)
        FROM   @xR.nodes('/i') AS n(x)
        WHERE  TRY_CAST(NULLIF(LTRIM(RTRIM(n.x.value('.', 'VARCHAR(20)'))), '') AS INT) IS NOT NULL;
    INSERT INTO @RSec(Id)
        SELECT DISTINCT TRY_CAST(NULLIF(LTRIM(RTRIM(n.x.value('.', 'VARCHAR(20)'))), '') AS INT)
        FROM   @xRS.nodes('/i') AS n(x)
        WHERE  TRY_CAST(NULLIF(LTRIM(RTRIM(n.x.value('.', 'VARCHAR(20)'))), '') AS INT) IS NOT NULL;

    DECLARE @hasLClass BIT = CASE WHEN EXISTS (SELECT 1 FROM @LClass) THEN 1 ELSE 0 END;
    DECLARE @hasLSec   BIT = CASE WHEN EXISTS (SELECT 1 FROM @LSec)   THEN 1 ELSE 0 END;
    DECLARE @hasRClass BIT = CASE WHEN EXISTS (SELECT 1 FROM @RClass) THEN 1 ELSE 0 END;
    DECLARE @hasRSec   BIT = CASE WHEN EXISTS (SELECT 1 FROM @RSec)   THEN 1 ELSE 0 END;

    -- ── Per (Teacher, Subject, Class, Section) subject percentage ───────────
    -- Identical metric to dbo.SpRPT_SMSExamTeacherWise: appeared students only,
    -- only active assignments, only Subject/Class/Section actually examined.
    IF OBJECT_ID('tempdb..#TS') IS NOT NULL DROP TABLE #TS;

    ;WITH SubjAgg AS
    (
        SELECT
            R.ClassID,
            R.SectionID,
            R.SubjectID,
            SUM(CASE WHEN R.IsAbsent = 0 THEN 1 ELSE 0 END)                               AS Appeared,
            SUM(CASE WHEN R.IsAbsent = 0 AND ISNULL(R.IsPassed, 0) = 1 THEN 1 ELSE 0 END) AS Passed,
            SUM(CASE WHEN R.IsAbsent = 0 THEN ISNULL(R.TotalMarks, 0)    ELSE 0 END) AS TotalMarks,
            SUM(CASE WHEN R.IsAbsent = 0 THEN ISNULL(R.MarksObtained, 0) ELSE 0 END) AS ObtainedMarks
        FROM        SMSExamResult R
        WHERE       R.ExamID   = @ExamID
          AND       R.IsActive = 1
          AND       EXISTS (SELECT 1 FROM SMSStudentAcademic SA
                            WHERE SA.StudentID = R.StudentID AND SA.IsActive = 1 AND SA.StatusID = 1)
        GROUP BY    R.ClassID, R.SectionID, R.SubjectID
    ),
    Assign AS
    (
        SELECT DISTINCT TeacherID, ClassID, SectionID, SubjectID
        FROM   SMSStaffSubject
        WHERE  ExamID   = @ExamID
          AND  IsActive = 1
    )
    SELECT
        SS.TeacherID,
        A.ClassID,
        A.SectionID,
        A.SubjectID,
        -- Average of Pass% by Pass Marks and Pass% by Obtained Marks — the exact
        -- cell percentage SpRPT_SMSExamTeacherWise returns, kept at the same
        -- DECIMAL(5,2) rounding so averaging these reproduces the Teacher Wise
        -- "Average %" value for the teacher.
        CAST(
            (
                CASE WHEN A.Appeared > 0
                     THEN A.Passed * 100.0 / A.Appeared
                     ELSE 0 END
                +
                CASE WHEN A.TotalMarks > 0
                     THEN A.ObtainedMarks * 100.0 / A.TotalMarks
                     ELSE 0 END
            ) / 2.0
        AS DECIMAL(5,2)) AS Pct
    INTO   #TS
    FROM        Assign  SS
    INNER JOIN  SubjAgg A ON A.ClassID   = SS.ClassID
                         AND A.SectionID = SS.SectionID
                         AND A.SubjectID = SS.SubjectID;

    -- ── Collapse to one row per (Side, Teacher) = average subject % ─────────
    IF OBJECT_ID('tempdb..#Res') IS NOT NULL DROP TABLE #Res;
    CREATE TABLE #Res
    (
        Side          CHAR(1),
        TeacherId     INT,
        TeacherName   NVARCHAR(200),
        ResultPercent DECIMAL(5,2)
    );

    -- LEFT scope
    INSERT INTO #Res (Side, TeacherId, TeacherName, ResultPercent)
    SELECT 'L', T.TeacherID, TC.VName, CAST(AVG(T.Pct) AS DECIMAL(5,2))
    FROM        #TS T
    INNER JOIN  Employees  TC ON TC.Id  = T.TeacherID
    WHERE       (@hasLClass = 0 OR T.ClassID   IN (SELECT Id FROM @LClass))
      AND       (@hasLSec   = 0 OR T.SectionID IN (SELECT Id FROM @LSec))
    GROUP BY    T.TeacherID, TC.VName;

    -- RIGHT scope
    INSERT INTO #Res (Side, TeacherId, TeacherName, ResultPercent)
    SELECT 'R', T.TeacherID, TC.VName, CAST(AVG(T.Pct) AS DECIMAL(5,2))
    FROM        #TS T
    INNER JOIN  Employees  TC ON TC.Id  = T.TeacherID
    WHERE       (@hasRClass = 0 OR T.ClassID   IN (SELECT Id FROM @RClass))
      AND       (@hasRSec   = 0 OR T.SectionID IN (SELECT Id FROM @RSec))
    GROUP BY    T.TeacherID, TC.VName;

    -- ── Final ranked output (per scope) ─────────────────────────────────────
    SELECT
        Side,
        ROW_NUMBER() OVER (PARTITION BY Side ORDER BY ResultPercent DESC, TeacherName) AS Sr,
        TeacherId,
        TeacherName,
        ResultPercent,
        DENSE_RANK() OVER (PARTITION BY Side ORDER BY ResultPercent DESC)               AS Position
    FROM   #Res
    ORDER BY Side, ResultPercent DESC, TeacherName;

    IF OBJECT_ID('tempdb..#TS')  IS NOT NULL DROP TABLE #TS;
    IF OBJECT_ID('tempdb..#Res') IS NOT NULL DROP TABLE #Res;
END
GO

USE [EasySchoolDB]
GO
/* ============================================================================
   SpRPT_SMSExamResultSheet
   ----------------------------------------------------------------------------
   Builds the Result Sheet for a Class + Section + Exam.

   Subjects differ from class to class, so the per-subject mark columns are
   produced with a DYNAMIC PIVOT (one column per subject that has marks for the
   selected exam/class/section).

   Returns 2 result sets:
     (1) Subject header   -> SubjectID, SubjectName, TotalMarks, PassMarks
                             (drives the column headers + the max/pass-mark rows)
     (2) Student rows     -> RollNo, StudentName, Position, CurrentPct, PrevPct,
                             Result, <one column per subject>, Total

   Parameters:
     @ExamID      INT  - the exam to report on              (required)
     @ClassID     INT  - class                              (required)
     @SectionID   INT  - section                            (required)
     @PrevExamID  INT  - previous exam for the "Prev%"      (optional, 0/NULL = none)

   Target: SQL Server 2019
   ============================================================================ */
IF OBJECT_ID('dbo.SpRPT_SMSExamResultSheet', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SpRPT_SMSExamResultSheet;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SpRPT_SMSExamResultSheet]
(
    @ExamID     INT,
    @ClassID    INT,
    @SectionID  INT,
    @PrevExamID INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    /* ---- 1) Subject header (static, safe even when there are no marks) ---- */
    SELECT
        SU.VID                  AS SubjectID,
        SU.VName                AS SubjectName,
        MAX(R.TotalMarks)       AS TotalMarks,
        MAX(R.PassMarks)        AS PassMarks
    FROM        SMSExamResult R
    INNER JOIN  SMSSubject     SU ON R.SubjectID = SU.VID
    WHERE       R.ExamID    = @ExamID
      AND       R.ClassID   = @ClassID
      AND       R.SectionID = @SectionID
      AND       R.IsActive  = 1
    GROUP BY    SU.VID, SU.VName
    ORDER BY    SU.VID;

    /* ---- Build the dynamic subject-column list (ordered by SubjectID) ---- */
    DECLARE @cols NVARCHAR(MAX);

    SELECT @cols = STRING_AGG(CONVERT(NVARCHAR(MAX), QUOTENAME(SubjectName)), ',')
                   WITHIN GROUP (ORDER BY SubjectID)
    FROM (
        SELECT DISTINCT SU.VID AS SubjectID, SU.VName AS SubjectName
        FROM        SMSExamResult R
        INNER JOIN  SMSSubject     SU ON R.SubjectID = SU.VID
        WHERE       R.ExamID    = @ExamID
          AND       R.ClassID   = @ClassID
          AND       R.SectionID = @SectionID
          AND       R.IsActive  = 1
    ) AS s;

    /* No marks for this filter -> return an empty student result set, then exit */
    IF @cols IS NULL
    BEGIN
        SELECT
            CAST(NULL AS NVARCHAR(50))   AS RollNo,
            CAST(NULL AS NVARCHAR(200))  AS StudentName,
            CAST(NULL AS NVARCHAR(200))  AS FatherName,
            CAST(NULL AS INT)            AS Position,
            CAST(NULL AS DECIMAL(5,2))   AS CurrentPct,
            CAST(NULL AS DECIMAL(5,2))   AS PrevPct,
            CAST(NULL AS NVARCHAR(10))   AS Result,
            CAST(NULL AS DECIMAL(9,2))   AS Total
        WHERE 1 = 0;
        RETURN;
    END

    /* ---- 2) Student rows (dynamic PIVOT) ---- */
    DECLARE @sql NVARCHAR(MAX);

    SET @sql = N'
    ;WITH Cur AS
    (
        SELECT  R.StudentID,
                SU.VName        AS SubjectName,
                R.MarksObtained,
                R.TotalMarks,
                R.IsAbsent,
                R.IsPassed
        FROM        SMSExamResult R
        INNER JOIN  SMSSubject     SU ON R.SubjectID = SU.VID
        WHERE       R.ExamID    = @ExamID
          AND       R.ClassID   = @ClassID
          AND       R.SectionID = @SectionID
          AND       R.IsActive  = 1
          AND       EXISTS (SELECT 1 FROM SMSStudentAcademic SA
                            WHERE SA.StudentID = R.StudentID AND SA.IsActive = 1 AND SA.StatusID = 1)
          -- On the Result Exclude List of this exam: left out of every result report.
          AND       NOT EXISTS (SELECT 1 FROM SMSExamExclude X
                                WHERE X.ExamID = R.ExamID AND X.StudentID = R.StudentID)
    ),
    Agg AS
    (
        SELECT  StudentID,
                SUM(ISNULL(MarksObtained, 0)) AS Obtained,
                SUM(ISNULL(TotalMarks, 0))    AS MaxMarks,
                SUM(CASE WHEN IsAbsent = 1 OR ISNULL(IsPassed, 0) = 0
                         THEN 1 ELSE 0 END)   AS FailCount
        FROM    Cur
        GROUP BY StudentID
    ),
    Prev AS
    (
        SELECT  R.StudentID,
                SUM(ISNULL(R.MarksObtained, 0)) AS Obtained,
                SUM(ISNULL(R.TotalMarks, 0))    AS MaxMarks
        FROM    SMSExamResult R
        WHERE   @PrevExamID IS NOT NULL AND @PrevExamID > 0
          AND   R.ExamID    = @PrevExamID
          AND   R.ClassID   = @ClassID
          AND   R.SectionID = @SectionID
          AND   R.IsActive  = 1
        GROUP BY R.StudentID
    ),
    Pvt AS
    (
        SELECT StudentID, ' + @cols + N'
        FROM (
            SELECT StudentID, SubjectName, MarksObtained FROM Cur
        ) AS src
        PIVOT ( MAX(MarksObtained) FOR SubjectName IN (' + @cols + N') ) AS p
    )
    SELECT
        ST.RollNo,
        ST.Name AS StudentName,
        PA.FatherName,
        -- Position is awarded only to students who passed every subject.
        -- Failed students (FailCount > 0) are excluded from the ranking (NULL).
        -- DENSE_RANK => tied students share the same position with NO gap
        -- (e.g. 1, 2, 2, 3 instead of 1, 2, 2, 4).
        CASE WHEN A.FailCount = 0
             THEN DENSE_RANK() OVER (
                      ORDER BY CASE WHEN A.FailCount = 0 THEN 0 ELSE 1 END,
                               A.Obtained DESC)
             ELSE NULL END                                         AS Position,
        CAST(CASE WHEN A.MaxMarks > 0 THEN A.Obtained * 100.0 / A.MaxMarks
                  ELSE 0 END AS DECIMAL(5,2))                       AS CurrentPct,
        CASE WHEN PR.MaxMarks > 0
             THEN CAST(PR.Obtained * 100.0 / PR.MaxMarks AS DECIMAL(5,2))
             ELSE NULL END                                         AS PrevPct,
        CASE WHEN A.FailCount = 0 THEN ''Pass'' ELSE ''Fail'' END  AS Result,
        ' + @cols + N',
        CAST(A.Obtained AS DECIMAL(9,2))                           AS Total
    FROM        Agg         A
    INNER JOIN  SMSStudent  ST ON A.StudentID = ST.VID
    LEFT  JOIN  SMSParent   PA ON ST.ParentID = PA.VID
    LEFT  JOIN  Prev        PR ON PR.StudentID = A.StudentID
    LEFT  JOIN  Pvt         P  ON P.StudentID  = A.StudentID
    ORDER BY    TRY_CONVERT(INT, ST.RollNo), ST.RollNo;';

    EXEC sp_executesql
        @sql,
        N'@ExamID INT, @ClassID INT, @SectionID INT, @PrevExamID INT',
        @ExamID, @ClassID, @SectionID, @PrevExamID;
END
GO

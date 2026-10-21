USE [EasySchoolDB]
GO
/* ============================================================================
   SpRPT_SMSExamStudentPosition
   ----------------------------------------------------------------------------
   Position-wise list of students for ONE Exam within ONE Class (across ALL
   sections of that class). Position is class-wide (DENSE_RANK by total obtained
   marks DESC) and — exactly like the Result Card — is awarded only to students
   who passed EVERY subject. Students who failed any subject get no position and
   are listed after the ranked ones (ordered by obtained marks).

   Returns one row per student:
       Sr, Position, RollNo, StudentName, FatherName, SectionName,
       TotalMarks, ObtainedMarks, Percent, Status

   Parameters:
     @ExamID  INT - the exam to report on   (required)
     @ClassID INT - the class to report on  (required)

   Target: SQL Server 2019
   ============================================================================ */
IF OBJECT_ID('dbo.SpRPT_SMSExamStudentPosition', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SpRPT_SMSExamStudentPosition;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SpRPT_SMSExamStudentPosition]
(
    @ExamID  INT,
    @ClassID INT
)
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Cur AS
    (
        SELECT  R.StudentID,
                R.SectionID,
                R.MarksObtained,
                R.TotalMarks,
                R.IsAbsent,
                R.IsPassed
        FROM        SMSExamResult R
        WHERE       R.ExamID   = @ExamID
          AND       R.IsActive = 1
          AND       R.ClassID  = @ClassID
          AND       EXISTS (SELECT 1 FROM SMSStudentAcademic SA
                            WHERE SA.StudentID = R.StudentID AND SA.IsActive = 1 AND SA.StatusID = 1)
          -- On the Result Exclude List of this exam: left out of every result report.
          AND       NOT EXISTS (SELECT 1 FROM SMSExamExclude X
                                WHERE X.ExamID = R.ExamID AND X.StudentID = R.StudentID)
    ),
    Agg AS
    (
        SELECT  StudentID,
                MIN(SectionID)                AS SectionID,
                SUM(ISNULL(MarksObtained, 0)) AS Obtained,
                SUM(ISNULL(TotalMarks, 0))    AS MaxMarks,
                SUM(CASE WHEN IsAbsent = 1 OR ISNULL(IsPassed, 0) = 0
                         THEN 1 ELSE 0 END)   AS FailCount
        FROM    Cur
        GROUP BY StudentID
    ),
    Ranked AS
    (
        SELECT  A.*,
                -- Position awarded only to students who passed every subject,
                -- ranked across the WHOLE class (DENSE_RANK => ties share a rank).
                CASE WHEN A.FailCount = 0
                     THEN DENSE_RANK() OVER (ORDER BY A.Obtained DESC)
                     ELSE NULL END AS Position
        FROM    Agg A
    )
    SELECT
        ROW_NUMBER() OVER (
            ORDER BY CASE WHEN RK.Position IS NULL THEN 1 ELSE 0 END,
                     RK.Position,
                     RK.Obtained DESC,
                     TRY_CONVERT(INT, ST.RollNo), ST.RollNo)   AS Sr,
        RK.Position,
        ST.RollNo,
        ST.Name                                                AS StudentName,
        PA.FatherName,
        SE.VName                                               AS SectionName,
        CAST(RK.MaxMarks AS DECIMAL(9,2))                      AS TotalMarks,
        CAST(RK.Obtained AS DECIMAL(9,2))                      AS ObtainedMarks,
        CAST(CASE WHEN RK.MaxMarks > 0
                  THEN RK.Obtained * 100.0 / RK.MaxMarks
                  ELSE 0 END AS DECIMAL(5,2))                  AS [Percent],
        CASE WHEN RK.FailCount = 0 THEN 'Pass' ELSE 'Fail' END AS Status
    FROM        Ranked     RK
    INNER JOIN  SMSStudent ST ON RK.StudentID = ST.VID
    LEFT  JOIN  SMSParent  PA ON ST.ParentID  = PA.VID
    LEFT  JOIN  SMSSection SE ON RK.SectionID = SE.VID
    -- Pass students first (by position / marks), failed students last; both % wise.
    ORDER BY    CASE WHEN RK.Position IS NULL THEN 1 ELSE 0 END,
                RK.Position,
                RK.Obtained DESC,
                TRY_CONVERT(INT, ST.RollNo), ST.RollNo;
END
GO

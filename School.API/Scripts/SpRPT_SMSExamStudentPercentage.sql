USE [EasySchoolDB]
GO
/* ============================================================================
   SpRPT_SMSExamStudentPercentage
   ----------------------------------------------------------------------------
   Percentage-wise list of students for ONE Exam within ONE Class (across ALL
   sections of that class). Unlike the Position report, ALL students — pass AND
   fail — are listed together strictly by percentage DESC. The Position column
   is still shown (class-wide DENSE_RANK by obtained marks, awarded only to
   students who passed every subject; NULL for failed students).

   Returns one row per student:
       Sr, Position, RollNo, StudentName, FatherName, SectionName,
       TotalMarks, ObtainedMarks, Percent, Status

   Parameters:
     @ExamID  INT - the exam to report on   (required)
     @ClassID INT - the class to report on  (required)

   Target: SQL Server 2019
   ============================================================================ */
IF OBJECT_ID('dbo.SpRPT_SMSExamStudentPercentage', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SpRPT_SMSExamStudentPercentage;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SpRPT_SMSExamStudentPercentage]
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
                -- Position: class-wide DENSE_RANK by obtained, only for full passers.
                CASE WHEN A.FailCount = 0
                     THEN DENSE_RANK() OVER (ORDER BY A.Obtained DESC)
                     ELSE NULL END AS Position,
                CAST(CASE WHEN A.MaxMarks > 0
                          THEN A.Obtained * 100.0 / A.MaxMarks
                          ELSE 0 END AS DECIMAL(5,2)) AS Pct
        FROM    Agg A
    )
    SELECT
        ROW_NUMBER() OVER (
            ORDER BY RK.Pct DESC,
                     RK.Obtained DESC,
                     TRY_CONVERT(INT, ST.RollNo), ST.RollNo)   AS Sr,
        RK.Position,
        ST.RollNo,
        ST.Name                                                AS StudentName,
        PA.FatherName,
        SE.VName                                               AS SectionName,
        CAST(RK.MaxMarks AS DECIMAL(9,2))                      AS TotalMarks,
        CAST(RK.Obtained AS DECIMAL(9,2))                      AS ObtainedMarks,
        RK.Pct                                                 AS [Percent],
        CASE WHEN RK.FailCount = 0 THEN 'Pass' ELSE 'Fail' END AS Status
    FROM        Ranked     RK
    INNER JOIN  SMSStudent ST ON RK.StudentID = ST.VID
    LEFT  JOIN  SMSParent  PA ON ST.ParentID  = PA.VID
    LEFT  JOIN  SMSSection SE ON RK.SectionID = SE.VID
    -- ALL students (pass + fail) together, strictly by percentage DESC.
    ORDER BY    RK.Pct DESC,
                RK.Obtained DESC,
                TRY_CONVERT(INT, ST.RollNo), ST.RollNo;
END
GO

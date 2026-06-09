USE [EasySchoolDB]
GO
/* ============================================================================
   SpRPT_SMSExamExecutive
   ----------------------------------------------------------------------------
   Executive (summary) report for ONE Exam within ONE Class + Section.

   Returns 2 result sets:
     (1) Overall    -> TotalStudents, TotalPass, TotalFail, ResultPct
                       (a student "passes" only if they passed EVERY subject)
     (2) Subjects   -> SubjectName, FailCount, FailPct, PassCount, PassPct,
                       TeacherName, Obtained, Total, Percent
                       (TeacherName comes from SMSClassTimetable -> SMSTeacher;
                        '-'/NULL until the timetable is populated)

   Parameters:
     @ExamID    INT - the exam     (required)
     @ClassID   INT - the class    (required)
     @SectionID INT - the section  (required)

   Target: SQL Server 2019
   ============================================================================ */
IF OBJECT_ID('dbo.SpRPT_SMSExamExecutive', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SpRPT_SMSExamExecutive;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SpRPT_SMSExamExecutive]
(
    @ExamID    INT,
    @ClassID   INT,
    @SectionID INT
)
AS
BEGIN
    SET NOCOUNT ON;

    /* ---- 1) Overall report ---- */
    ;WITH StuAgg AS
    (
        SELECT  R.StudentID,
                SUM(CASE WHEN R.IsAbsent = 1 OR ISNULL(R.IsPassed, 0) = 0
                         THEN 1 ELSE 0 END) AS FailCount
        FROM    SMSExamResult R
        WHERE   R.ExamID    = @ExamID
          AND   R.IsActive  = 1
          AND   R.ClassID   = @ClassID
          AND   R.SectionID = @SectionID
          AND   EXISTS (SELECT 1 FROM SMSStudentAcademic SA
                        WHERE SA.StudentID = R.StudentID AND SA.IsActive = 1 AND SA.StatusID = 1)
        GROUP BY R.StudentID
    )
    SELECT
        COUNT(*)                                              AS TotalStudents,
        SUM(CASE WHEN FailCount = 0 THEN 1 ELSE 0 END)        AS TotalPass,
        SUM(CASE WHEN FailCount > 0 THEN 1 ELSE 0 END)        AS TotalFail,
        CAST(CASE WHEN COUNT(*) > 0
                  THEN SUM(CASE WHEN FailCount = 0 THEN 1 ELSE 0 END) * 100.0 / COUNT(*)
                  ELSE 0 END AS DECIMAL(5,2))                 AS ResultPct
    FROM StuAgg;

    /* ---- 2) Subject wise status + teacher percentage ---- */
    SELECT
        SU.VName                                              AS SubjectName,
        SUM(CASE WHEN R.IsAbsent = 1 OR ISNULL(R.IsPassed, 0) = 0
                 THEN 1 ELSE 0 END)                           AS FailCount,
        CAST(CASE WHEN COUNT(*) > 0
                  THEN SUM(CASE WHEN R.IsAbsent = 1 OR ISNULL(R.IsPassed, 0) = 0
                                THEN 1 ELSE 0 END) * 100.0 / COUNT(*)
                  ELSE 0 END AS DECIMAL(5,2))                 AS FailPct,
        SUM(CASE WHEN R.IsAbsent = 0 AND ISNULL(R.IsPassed, 0) = 1
                 THEN 1 ELSE 0 END)                           AS PassCount,
        CAST(CASE WHEN COUNT(*) > 0
                  THEN SUM(CASE WHEN R.IsAbsent = 0 AND ISNULL(R.IsPassed, 0) = 1
                                THEN 1 ELSE 0 END) * 100.0 / COUNT(*)
                  ELSE 0 END AS DECIMAL(5,2))                 AS PassPct,
        (SELECT TOP 1 T.VName
         FROM   SMSClassTimetable TT
         INNER JOIN SMSTeacher    T ON T.VID = TT.TeacherID
         WHERE  TT.ClassID   = @ClassID
           AND  TT.SectionID = @SectionID
           AND  TT.SubjectID = SU.VID
           AND  TT.IsActive  = 1)                             AS TeacherName,
        CAST(SUM(ISNULL(R.MarksObtained, 0)) AS DECIMAL(9,2)) AS Obtained,
        CAST(SUM(ISNULL(R.TotalMarks, 0))    AS DECIMAL(9,2)) AS Total,
        CAST(CASE WHEN SUM(ISNULL(R.TotalMarks, 0)) > 0
                  THEN SUM(ISNULL(R.MarksObtained, 0)) * 100.0 / SUM(ISNULL(R.TotalMarks, 0))
                  ELSE 0 END AS DECIMAL(5,2))                 AS [Percent]
    FROM        SMSExamResult R
    INNER JOIN  SMSSubject    SU ON R.SubjectID = SU.VID
    WHERE       R.ExamID    = @ExamID
      AND       R.IsActive  = 1
      AND       R.ClassID   = @ClassID
      AND       R.SectionID = @SectionID
      AND       EXISTS (SELECT 1 FROM SMSStudentAcademic SA
                        WHERE SA.StudentID = R.StudentID AND SA.IsActive = 1 AND SA.StatusID = 1)
    GROUP BY    SU.VID, SU.VName
    ORDER BY    SU.VName;
END
GO

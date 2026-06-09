USE [EasySchoolDB]
GO
/* ============================================================================
   SpRPT_SMSExamResultAnnouncement
   ----------------------------------------------------------------------------
   Result Announcement report for ONE Exam across the WHOLE school.

   A student "passes" only if they passed EVERY subject in the exam.

   Returns 4 result sets:
     (1) Overall      -> TotalStudents, TotalPass, TotalFail, PassPct, FailPct
     (2) Campus wise  -> Name(=Campus), TotalStudents, TotalPass, TotalFail, PassPct, FailPct
     (3) Class wise   -> Name(=Class),  TotalStudents, TotalPass, TotalFail, PassPct, FailPct
     (4) Top positions-> ClassName, Position(1-3), RollNo, StudentName,
                         SectionName, ObtainedMarks, Percent
                         (class-wide DENSE_RANK among full passers; top 3 each class)

   Parameters:
     @ExamID INT - the exam to announce (required)

   Target: SQL Server 2019
   ============================================================================ */
IF OBJECT_ID('dbo.SpRPT_SMSExamResultAnnouncement', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SpRPT_SMSExamResultAnnouncement;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SpRPT_SMSExamResultAnnouncement]
(
    @ExamID INT
)
AS
BEGIN
    SET NOCOUNT ON;

    /* Per-student aggregate for the exam (whole school). */
    CREATE TABLE #Stu
    (
        StudentID INT,
        ClassID   INT,
        SectionID INT,
        CampusID  INT,
        Obtained  DECIMAL(18,2),
        MaxMarks  DECIMAL(18,2),
        FailCount INT
    );

    INSERT INTO #Stu (StudentID, ClassID, SectionID, CampusID, Obtained, MaxMarks, FailCount)
    SELECT  R.StudentID,
            MIN(R.ClassID),
            MIN(R.SectionID),
            MAX(ISNULL(ST.CampusID, 0)),
            SUM(ISNULL(R.MarksObtained, 0)),
            SUM(ISNULL(R.TotalMarks, 0)),
            SUM(CASE WHEN R.IsAbsent = 1 OR ISNULL(R.IsPassed, 0) = 0 THEN 1 ELSE 0 END)
    FROM        SMSExamResult R
    INNER JOIN  SMSStudent    ST ON ST.VID = R.StudentID
    WHERE       R.ExamID   = @ExamID
      AND       R.IsActive = 1
      AND       EXISTS (SELECT 1 FROM SMSStudentAcademic SA
                        WHERE SA.StudentID = R.StudentID AND SA.IsActive = 1 AND SA.StatusID = 1)
    GROUP BY    R.StudentID;

    /* ---- 1) Overall (whole school) ---- */
    SELECT
        COUNT(*)                                          AS TotalStudents,
        SUM(CASE WHEN FailCount = 0 THEN 1 ELSE 0 END)    AS TotalPass,
        SUM(CASE WHEN FailCount > 0 THEN 1 ELSE 0 END)    AS TotalFail,
        CAST(CASE WHEN COUNT(*) > 0 THEN SUM(CASE WHEN FailCount = 0 THEN 1 ELSE 0 END) * 100.0 / COUNT(*) ELSE 0 END AS DECIMAL(5,2)) AS PassPct,
        CAST(CASE WHEN COUNT(*) > 0 THEN SUM(CASE WHEN FailCount > 0 THEN 1 ELSE 0 END) * 100.0 / COUNT(*) ELSE 0 END AS DECIMAL(5,2)) AS FailPct
    FROM #Stu;

    /* ---- 2) Campus wise ---- */
    SELECT
        ISNULL(C.VName, 'Unknown')                        AS Name,
        COUNT(*)                                          AS TotalStudents,
        SUM(CASE WHEN S.FailCount = 0 THEN 1 ELSE 0 END)  AS TotalPass,
        SUM(CASE WHEN S.FailCount > 0 THEN 1 ELSE 0 END)  AS TotalFail,
        CAST(CASE WHEN COUNT(*) > 0 THEN SUM(CASE WHEN S.FailCount = 0 THEN 1 ELSE 0 END) * 100.0 / COUNT(*) ELSE 0 END AS DECIMAL(5,2)) AS PassPct,
        CAST(CASE WHEN COUNT(*) > 0 THEN SUM(CASE WHEN S.FailCount > 0 THEN 1 ELSE 0 END) * 100.0 / COUNT(*) ELSE 0 END AS DECIMAL(5,2)) AS FailPct
    FROM        #Stu       S
    LEFT  JOIN  SMSCampus  C ON C.VID = S.CampusID
    GROUP BY    C.VID, C.VName
    ORDER BY    C.VName;

    /* ---- 3) Class wise ---- */
    SELECT
        ISNULL(CL.VName, 'Unknown')                       AS Name,
        COUNT(*)                                          AS TotalStudents,
        SUM(CASE WHEN S.FailCount = 0 THEN 1 ELSE 0 END)  AS TotalPass,
        SUM(CASE WHEN S.FailCount > 0 THEN 1 ELSE 0 END)  AS TotalFail,
        CAST(CASE WHEN COUNT(*) > 0 THEN SUM(CASE WHEN S.FailCount = 0 THEN 1 ELSE 0 END) * 100.0 / COUNT(*) ELSE 0 END AS DECIMAL(5,2)) AS PassPct,
        CAST(CASE WHEN COUNT(*) > 0 THEN SUM(CASE WHEN S.FailCount > 0 THEN 1 ELSE 0 END) * 100.0 / COUNT(*) ELSE 0 END AS DECIMAL(5,2)) AS FailPct
    FROM        #Stu      S
    LEFT  JOIN  SMSClass  CL ON CL.VID = S.ClassID
    GROUP BY    CL.VID, CL.VName
    ORDER BY    CL.VID;

    /* ---- 4) Top 1st/2nd/3rd positions per class ---- */
    ;WITH Ranked AS
    (
        SELECT  S.*,
                DENSE_RANK() OVER (PARTITION BY S.ClassID ORDER BY S.Obtained DESC) AS Position
        FROM    #Stu S
        WHERE   S.FailCount = 0
    )
    SELECT
        ISNULL(CL.VName, 'Unknown')                        AS ClassName,
        RK.Position,
        ST.RollNo,
        ST.Name                                            AS StudentName,
        PA.FatherName,
        SE.VName                                           AS SectionName,
        CAST(RK.MaxMarks AS DECIMAL(9,2))                  AS TotalMarks,
        CAST(RK.Obtained AS DECIMAL(9,2))                  AS ObtainedMarks,
        CAST(CASE WHEN RK.MaxMarks > 0 THEN RK.Obtained * 100.0 / RK.MaxMarks ELSE 0 END AS DECIMAL(5,2)) AS [Percent]
    FROM        Ranked     RK
    INNER JOIN  SMSStudent ST ON ST.VID = RK.StudentID
    LEFT  JOIN  SMSParent  PA ON ST.ParentID  = PA.VID
    LEFT  JOIN  SMSClass   CL ON CL.VID = RK.ClassID
    LEFT  JOIN  SMSSection SE ON SE.VID = RK.SectionID
    WHERE       RK.Position <= 3
    ORDER BY    CL.VID, RK.Position, RK.Obtained DESC, TRY_CONVERT(INT, ST.RollNo), ST.RollNo;

    DROP TABLE #Stu;
END
GO

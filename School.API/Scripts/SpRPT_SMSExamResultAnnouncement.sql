USE [EasySchoolDB]
GO
/* ============================================================================
   SpRPT_SMSExamResultAnnouncement
   ----------------------------------------------------------------------------
   Result Announcement report for ONE Exam.

   Scope is controlled by two OPTIONAL comma-separated filters:
     - @SectionIDs set -> only those sections        (1 or many sections)
     - @ClassIDs   set -> only those classes         (1 or many classes)
     - both NULL/empty -> WHOLE school               (default)
   @SectionIDs takes precedence over @ClassIDs when both are supplied.

   A student "passes" only if they passed EVERY subject in the exam.

   Returns 4 result sets:
     (1) Overall      -> TotalStudents, TotalPass, TotalFail, PassPct, FailPct
     (2) Campus wise  -> Name(=Campus), TotalStudents, TotalPass, TotalFail, PassPct, FailPct
     (3) Class wise   -> Name(=Class),  TotalStudents, TotalPass, TotalFail, PassPct, FailPct
     (4) Top positions-> ClassName, Scope, ScopeOrder, Position(1-3), RollNo,
                         StudentName, SectionName, ObtainedMarks, Percent
                         For EACH class three blocks are returned, in order:
                           Scope='Overall' -> top 3 of the whole class (all campuses)
                           Scope='Girls'   -> top 3 of the Girls campus students
                           Scope='Boys'    -> top 3 of the Boys campus students
                         (DENSE_RANK among full passers within each scope; top 3)
                         Boys/Girls is derived from the student's Campus name.

   Parameters:
     @ExamID     INT          - the exam to announce (required)
     @ClassIDs   VARCHAR(MAX) - optional CSV of ClassIDs   (NULL/'' = all)
     @SectionIDs VARCHAR(MAX) - optional CSV of SectionIDs (NULL/'' = all)

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
    @ExamID     INT,
    @ClassIDs   VARCHAR(MAX) = NULL,
    @SectionIDs VARCHAR(MAX) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    /* Normalise empty strings to NULL so the filters below switch off cleanly. */
    IF (LTRIM(RTRIM(ISNULL(@ClassIDs,   ''))) = '') SET @ClassIDs   = NULL;
    IF (LTRIM(RTRIM(ISNULL(@SectionIDs, ''))) = '') SET @SectionIDs = NULL;

    /* Parse the CSV filters into integer key tables (empty when filter is off).
       Uses XML shredding instead of STRING_SPLIT so it works on databases whose
       compatibility level is below 130 (STRING_SPLIT requires 130+). The IDs are
       pure integers, so the XML conversion is safe (no special characters). */
    DECLARE @ClassFilter   TABLE (Id INT PRIMARY KEY);
    DECLARE @SectionFilter TABLE (Id INT PRIMARY KEY);

    IF (@ClassIDs IS NOT NULL)
    BEGIN
        DECLARE @ClassXml XML = CAST('<i>' + REPLACE(@ClassIDs, ',', '</i><i>') + '</i>' AS XML);
        INSERT INTO @ClassFilter (Id)
        SELECT DISTINCT TRY_CONVERT(INT, LTRIM(RTRIM(n.value('.', 'VARCHAR(20)'))))
        FROM   @ClassXml.nodes('/i') AS t(n)
        WHERE  TRY_CONVERT(INT, LTRIM(RTRIM(n.value('.', 'VARCHAR(20)')))) IS NOT NULL;
    END

    IF (@SectionIDs IS NOT NULL)
    BEGIN
        DECLARE @SectionXml XML = CAST('<i>' + REPLACE(@SectionIDs, ',', '</i><i>') + '</i>' AS XML);
        INSERT INTO @SectionFilter (Id)
        SELECT DISTINCT TRY_CONVERT(INT, LTRIM(RTRIM(n.value('.', 'VARCHAR(20)'))))
        FROM   @SectionXml.nodes('/i') AS t(n)
        WHERE  TRY_CONVERT(INT, LTRIM(RTRIM(n.value('.', 'VARCHAR(20)')))) IS NOT NULL;
    END

    /* Per-student aggregate for the exam (scoped by the optional filters). */
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
      /* Section filter wins when supplied; otherwise apply the class filter. */
      AND       (@SectionIDs IS NOT NULL
                    OR @ClassIDs IS NULL
                    OR R.ClassID IN (SELECT Id FROM @ClassFilter))
      AND       (@SectionIDs IS NULL
                    OR R.SectionID IN (SELECT Id FROM @SectionFilter))
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

    /* ---- 4) Top 1st/2nd/3rd positions per class ----
       For every class we return three position blocks: the whole class
       (Overall), then the Girls campus, then the Boys campus. Boys/Girls is
       decided from the student's Campus name (robust against dirty Gender data). */
    ;WITH Passers AS
    (
        SELECT  S.StudentID, S.ClassID, S.SectionID, S.Obtained, S.MaxMarks,
                CASE WHEN C.VName LIKE '%Girl%' THEN 'Girls'
                     WHEN C.VName LIKE '%Boy%'  THEN 'Boys'
                     ELSE NULL END                          AS GenderScope
        FROM        #Stu      S
        LEFT  JOIN  SMSCampus C ON C.VID = S.CampusID
        WHERE       S.FailCount = 0
    ),
    Scoped AS
    (
        /* Overall — whole class, all campuses */
        SELECT  P.StudentID, P.ClassID, P.SectionID, P.Obtained, P.MaxMarks,
                CAST('Overall' AS VARCHAR(10)) AS Scope, 0 AS ScopeOrder,
                DENSE_RANK() OVER (PARTITION BY P.ClassID ORDER BY P.Obtained DESC) AS Position
        FROM    Passers P

        UNION ALL
        /* Girls campus only */
        SELECT  P.StudentID, P.ClassID, P.SectionID, P.Obtained, P.MaxMarks,
                CAST('Girls' AS VARCHAR(10)) AS Scope, 1 AS ScopeOrder,
                DENSE_RANK() OVER (PARTITION BY P.ClassID ORDER BY P.Obtained DESC) AS Position
        FROM    Passers P
        WHERE   P.GenderScope = 'Girls'

        UNION ALL
        /* Boys campus only */
        SELECT  P.StudentID, P.ClassID, P.SectionID, P.Obtained, P.MaxMarks,
                CAST('Boys' AS VARCHAR(10)) AS Scope, 2 AS ScopeOrder,
                DENSE_RANK() OVER (PARTITION BY P.ClassID ORDER BY P.Obtained DESC) AS Position
        FROM    Passers P
        WHERE   P.GenderScope = 'Boys'
    )
    SELECT
        ISNULL(CL.VName, 'Unknown')                        AS ClassName,
        SC.Scope,
        SC.ScopeOrder,
        SC.Position,
        ST.RollNo,
        ST.Name                                            AS StudentName,
        PA.FatherName,
        SE.VName                                           AS SectionName,
        CAST(SC.MaxMarks AS DECIMAL(9,2))                  AS TotalMarks,
        CAST(SC.Obtained AS DECIMAL(9,2))                  AS ObtainedMarks,
        CAST(CASE WHEN SC.MaxMarks > 0 THEN SC.Obtained * 100.0 / SC.MaxMarks ELSE 0 END AS DECIMAL(5,2)) AS [Percent]
    FROM        Scoped     SC
    INNER JOIN  SMSStudent ST ON ST.VID = SC.StudentID
    LEFT  JOIN  SMSParent  PA ON ST.ParentID  = PA.VID
    LEFT  JOIN  SMSClass   CL ON CL.VID = SC.ClassID
    LEFT  JOIN  SMSSection SE ON SE.VID = SC.SectionID
    WHERE       SC.Position <= 3
    ORDER BY    CL.VID, SC.ScopeOrder, SC.Position, SC.Obtained DESC, TRY_CONVERT(INT, ST.RollNo), ST.RollNo;

    DROP TABLE #Stu;
END
GO

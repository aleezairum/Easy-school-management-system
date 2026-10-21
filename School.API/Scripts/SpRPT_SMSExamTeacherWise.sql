/* ============================================================================
   SpRPT_SMSExamTeacherWise
   ----------------------------------------------------------------------------
   Teacher Wise Result report for ONE Exam.  The report is driven by the
   teacher → subject ASSIGNMENTS (dbo.SMSStaffSubject, the "Staff Subjects"
   screen): for each teacher we show the exam result of every subject that is
   assigned to them.

   Returns 1 result set — one row per (Teacher, Subject, Class, Section) that is
   BOTH assigned to the teacher AND has exam results for the chosen exam.  The
   application layer groups these flat rows by teacher, so each teacher becomes
   a single report row whose cells are their individual subject results and
   whose "Average %" is the mean of those cell percentages.

     TeacherId, TeacherName,
     SubjectId, SubjectName,
     ClassId,   ClassName,
     SectionId, SectionName,
     ColumnLabel    ("<Subject> <Class>-<Section>", e.g. "Physics 9-B"),
     Appeared,
     TotalMarks     (SUM of subject max marks, appeared only),
     ObtainedMarks  (SUM of obtained marks,    appeared only),
     Percent        Average of:
                       - Pass % by Pass Marks     (Passed   / Appeared    * 100)
                       - Pass % by Obtained Marks  (Obtained / TotalMarks  * 100)
                     i.e. (PassPercentByPassMarks + PassPercentByObtainedMarks) / 2,
                     same "Average%" definition used by SpRPT_SMSExamSubjectWise.

   Students on the exam's Result Exclude List (dbo.SMSExamExclude, created by
   ExamResultControls.sql) are left out.

   A student "appears" when IsAbsent = 0 (same rule as the other exam reports).
   Only ACTIVE assignments BELONGING TO @ExamID are considered — assignments are
   per-exam (Staff Subjects screen), so an exam with none entered or copied into
   it reports no teachers. An assignment only shows up when that
   Subject/Class/Section was actually examined in @ExamID (INNER JOIN on the
   result aggregate — no empty 0% cells).

   Parameters:
     @ExamID    INT - the exam                          (required)
     @TeacherID INT - narrow to one teacher (NULL/0 = all teachers)

   Target: SQL Server 2019
   ============================================================================ */
IF OBJECT_ID('dbo.SpRPT_SMSExamTeacherWise', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SpRPT_SMSExamTeacherWise;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SpRPT_SMSExamTeacherWise]
(
    @ExamID    INT,
    @TeacherID INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    -- Treat 0 the same as "no filter" (the web layer sends 0 when unselected).
    IF @TeacherID = 0 SET @TeacherID = NULL;

    -- One aggregate per Subject + Class + Section (appeared students only).
    ;WITH SubjAgg AS
    (
        SELECT
            R.ClassID,
            R.SectionID,
            R.SubjectID,
            SUM(CASE WHEN R.IsAbsent = 0 THEN 1 ELSE 0 END)                                        AS Appeared,
            SUM(CASE WHEN R.IsAbsent = 0 AND ISNULL(R.IsPassed, 0) = 1 THEN 1 ELSE 0 END)           AS Passed,
            SUM(CASE WHEN R.IsAbsent = 0 THEN ISNULL(R.TotalMarks, 0)    ELSE 0 END)                AS TotalMarks,
            SUM(CASE WHEN R.IsAbsent = 0 THEN ISNULL(R.MarksObtained, 0) ELSE 0 END)                AS ObtainedMarks
        FROM        SMSExamResult R
        WHERE       R.ExamID   = @ExamID
          AND       R.IsActive = 1
          AND       EXISTS (SELECT 1 FROM SMSStudentAcademic SA
                            WHERE SA.StudentID = R.StudentID AND SA.IsActive = 1 AND SA.StatusID = 1)
          -- Left out of the teachers' result for this exam (Result Exclude List).
          AND       NOT EXISTS (SELECT 1 FROM SMSExamExclude X
                                WHERE X.ExamID = @ExamID AND X.StudentID = R.StudentID)
        GROUP BY    R.ClassID, R.SectionID, R.SubjectID
    ),
    -- Distinct teacher → subject assignments (collapse multiple LectureNo rows).
    Assign AS
    (
        SELECT DISTINCT TeacherID, ClassID, SectionID, SubjectID
        FROM   SMSStaffSubject
        WHERE  ExamID   = @ExamID
          AND  IsActive = 1
          AND  (@TeacherID IS NULL OR TeacherID = @TeacherID)
    )
    SELECT
        SS.TeacherID                          AS TeacherId,
        T.VName                               AS TeacherName,
        A.SubjectID                           AS SubjectId,
        SU.VName                              AS SubjectName,
        A.ClassID                             AS ClassId,
        CL.VName                              AS ClassName,
        A.SectionID                           AS SectionId,
        SE.VName                              AS SectionName,
        SU.VName
            + CASE WHEN CL.VName IS NULL THEN '' ELSE ' ' + CL.VName END
            + CASE WHEN SE.VName IS NULL THEN '' ELSE '-' + SE.VName END AS ColumnLabel,
        A.Appeared,
        CAST(A.TotalMarks    AS DECIMAL(9,2)) AS TotalMarks,
        CAST(A.ObtainedMarks AS DECIMAL(9,2)) AS ObtainedMarks,
        -- Average of Pass% by Pass Marks and Pass% by Obtained Marks (matches
        -- the "Average%" shown in SpRPT_SMSExamSubjectWise).
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
        AS DECIMAL(5,2)) AS [Percent]
    FROM        Assign     SS
    INNER JOIN  SubjAgg    A  ON A.ClassID   = SS.ClassID
                             AND A.SectionID = SS.SectionID
                             AND A.SubjectID = SS.SubjectID
    INNER JOIN  Employees  T  ON T.Id   = SS.TeacherID
    INNER JOIN  SMSSubject SU ON SU.VID = A.SubjectID
    LEFT  JOIN  SMSClass   CL ON CL.VID = A.ClassID
    LEFT  JOIN  SMSSection SE ON SE.VID = A.SectionID
    ORDER BY    T.VName, CL.VName, SU.VName;
END
GO

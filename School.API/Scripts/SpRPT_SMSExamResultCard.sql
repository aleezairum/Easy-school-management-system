USE [EasySchoolDB]
GO
/* ============================================================================
   SpRPT_SMSExamResultCard
   ----------------------------------------------------------------------------
   Builds the per-student Result Card for an Exam, with an optional scope.
   (Sister report of SpRPT_SMSExamResultSheet — the sheet is one row per
    student; the card is one printable card per student with full subject
    detail.)

   Scope is flexible — pass 0 to widen it:
       Whole school  : @ClassID = 0, @SectionID = 0, @StudentID = 0
       One class     : @ClassID = X, @SectionID = 0, @StudentID = 0
       One section   : @ClassID = X, @SectionID = Y, @StudentID = 0
       One student   : @StudentID = Z   (class/section optional)

   Position, TotalStudents and HighestMarks are ALWAYS calculated within each
   student's OWN Class group (across all sections, via window functions), so
   they stay correct no matter how wide the scope is — even when printing the
   whole school in one run, and even when filtered to a single student.

   Returns 2 result sets:
     (1) Student headers  -> StudentID, RollNo, StudentName, FatherName,
                             ClassName, SectionName, Position, TotalStudents,
                             HighestMarks, CurrentPct, PrevPct, GrandTotal,
                             GrandPass, GrandObt, GrandStatus,
                             PresentDays, SchoolDays, AttendancePct
     (2) Subject details  -> StudentID, SubjectName, VDate, TotalMarks,
                             PassMarks, MarksObtained, Status

   Parameters:
     @ExamID      INT  - the exam to report on              (required)
     @ClassID     INT  - class       (optional, 0 = all classes)
     @SectionID   INT  - section     (optional, 0 = all sections)
     @PrevExamID  INT  - previous exam for the "Prev%"      (optional, 0/NULL = none)
     @StudentID   INT  - single student to print            (optional, 0 = all in scope)

   Target: SQL Server 2019
   ============================================================================ */
IF OBJECT_ID('dbo.SpRPT_SMSExamResultCard', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SpRPT_SMSExamResultCard;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SpRPT_SMSExamResultCard]
(
    @ExamID     INT,
    @ClassID    INT = 0,
    @SectionID  INT = 0,
    @PrevExamID INT = NULL,
    @StudentID  INT = 0
)
AS
BEGIN
    SET NOCOUNT ON;

    /* ---- Attendance window ----------------------------------------------
       Attendance is counted from the day AFTER the previous term's last paper
       up to and including this term's last paper. If no previous exam is given,
       the lower bound is open (counts from the start of available attendance). */
    DECLARE @ThisLastPaper DATE, @PrevLastPaper DATE;

    SELECT @ThisLastPaper = MAX(R.VDate)
    FROM   SMSExamResult R
    WHERE  R.ExamID   = @ExamID
      AND  R.IsActive = 1
      AND  (@ClassID   = 0 OR R.ClassID   = @ClassID)
      AND  (@SectionID = 0 OR R.SectionID = @SectionID);

    IF (@PrevExamID IS NOT NULL AND @PrevExamID > 0)
        SELECT @PrevLastPaper = MAX(R.VDate)
        FROM   SMSExamResult R
        WHERE  R.ExamID   = @PrevExamID
          AND  R.IsActive = 1
          AND  (@ClassID   = 0 OR R.ClassID   = @ClassID)
          AND  (@SectionID = 0 OR R.SectionID = @SectionID);

    /* ---- Per-student aggregates for the selected exam (within scope) ----
       NOTE: the student filter is NOT applied here, so each student's group
       stats (position, total, highest) reflect the WHOLE Class. */
    ;WITH Cur AS
    (
        SELECT  R.StudentID,
                R.ClassID,
                R.SectionID,
                R.MarksObtained,
                R.TotalMarks,
                R.PassMarks,
                R.IsAbsent,
                R.IsPassed
        FROM        SMSExamResult R
        WHERE       R.ExamID    = @ExamID
          AND       R.IsActive  = 1
          AND       (@ClassID   = 0 OR R.ClassID   = @ClassID)
          AND       (@SectionID = 0 OR R.SectionID = @SectionID)
          AND       EXISTS (SELECT 1 FROM SMSStudentAcademic SA
                            WHERE SA.StudentID = R.StudentID AND SA.IsActive = 1 AND SA.StatusID = 1)
    ),
    Agg AS
    (
        SELECT  StudentID,
                MIN(ClassID)   AS ClassID,
                MIN(SectionID) AS SectionID,
                SUM(ISNULL(MarksObtained, 0)) AS Obtained,
                SUM(ISNULL(TotalMarks, 0))    AS MaxMarks,
                SUM(ISNULL(PassMarks, 0))     AS PassMarksTotal,
                SUM(CASE WHEN IsAbsent = 1 OR ISNULL(IsPassed, 0) = 0
                         THEN 1 ELSE 0 END)   AS FailCount
        FROM    Cur
        GROUP BY StudentID
    ),
    Ranked AS
    (
        SELECT  A.*,
                -- Position is awarded only to students who passed every subject,
                -- ranked within their own Class (across all sections)
                -- (DENSE_RANK => ties share a position with NO gap).
                CASE WHEN A.FailCount = 0
                     THEN DENSE_RANK() OVER (
                              PARTITION BY A.ClassID
                              ORDER BY A.Obtained DESC)
                     ELSE NULL END                                       AS Position,
                COUNT(*)        OVER (PARTITION BY A.ClassID) AS TotalStudents,
                MAX(A.Obtained) OVER (PARTITION BY A.ClassID) AS HighestMarks
        FROM    Agg A
    ),
    Prev AS
    (
        SELECT  R.StudentID,
                SUM(ISNULL(R.MarksObtained, 0)) AS Obtained,
                SUM(ISNULL(R.TotalMarks, 0))    AS MaxMarks
        FROM    SMSExamResult R
        WHERE   @PrevExamID IS NOT NULL AND @PrevExamID > 0
          AND   R.ExamID    = @PrevExamID
          AND   R.IsActive  = 1
          AND   (@ClassID   = 0 OR R.ClassID   = @ClassID)
          AND   (@SectionID = 0 OR R.SectionID = @SectionID)
        GROUP BY R.StudentID
    ),
    Att AS
    (
        /* Per-student attendance within the [PrevLastPaper+1 .. ThisLastPaper] window.
           SchoolDays  = days attendance was taken for the student (any status).
           PresentDays = days the student was marked Present (Attendence = 1).
           Days BEFORE the student's AdmissionDate are excluded, so a student admitted
           mid-session is never counted absent (in SchoolDays) for days they were not
           yet enrolled — this keeps the printed attendance % correct even if stray
           absent rows were entered for those earlier days. */
        SELECT  A.StudentID,
                COUNT(*)                                              AS SchoolDays,
                SUM(CASE WHEN A.Attendence = 1 THEN 1 ELSE 0 END)     AS PresentDays
        FROM        StdAttendence A
        INNER JOIN  SMSStudent    S ON S.VID = A.StudentID
        WHERE   (@ThisLastPaper IS NULL OR A.VDate <= @ThisLastPaper)
          AND   (@PrevLastPaper IS NULL OR A.VDate >  @PrevLastPaper)
          AND   (S.AdmissionDate IS NULL OR A.VDate >= CAST(S.AdmissionDate AS DATE))
        GROUP BY A.StudentID
    )
    /* ---- 1) Student headers ---- */
    SELECT
        ST.VID                  AS StudentID,
        ST.RollNo,
        ST.Name                 AS StudentName,
        PA.FatherName,
        CL.VName                AS ClassName,
        SE.VName                AS SectionName,
        RK.Position,
        RK.TotalStudents,
        RK.HighestMarks,
        CAST(CASE WHEN RK.MaxMarks > 0 THEN RK.Obtained * 100.0 / RK.MaxMarks
                  ELSE 0 END AS DECIMAL(5,2))                       AS CurrentPct,
        CASE WHEN PR.MaxMarks > 0
             THEN CAST(PR.Obtained * 100.0 / PR.MaxMarks AS DECIMAL(5,2))
             ELSE NULL END                                         AS PrevPct,
        CAST(RK.MaxMarks       AS DECIMAL(9,2))                    AS GrandTotal,
        CAST(RK.PassMarksTotal AS DECIMAL(9,2))                    AS GrandPass,
        CAST(RK.Obtained       AS DECIMAL(9,2))                    AS GrandObt,
        CASE WHEN RK.FailCount = 0 THEN 'Pass' ELSE 'Fail' END     AS GrandStatus,
        ISNULL(AT.PresentDays, 0)                                  AS PresentDays,
        ISNULL(AT.SchoolDays,  0)                                  AS SchoolDays,
        CAST(CASE WHEN ISNULL(AT.SchoolDays, 0) > 0
                  THEN AT.PresentDays * 100.0 / AT.SchoolDays
                  ELSE 0 END AS DECIMAL(5,2))                      AS AttendancePct
    FROM        Ranked      RK
    INNER JOIN  SMSStudent  ST ON RK.StudentID = ST.VID
    LEFT  JOIN  SMSParent   PA ON ST.ParentID  = PA.VID
    LEFT  JOIN  SMSClass    CL ON RK.ClassID   = CL.VID
    LEFT  JOIN  SMSSection  SE ON RK.SectionID = SE.VID
    LEFT  JOIN  Prev        PR ON PR.StudentID = RK.StudentID
    LEFT  JOIN  Att         AT ON AT.StudentID = RK.StudentID
    WHERE       (@StudentID = 0 OR ST.VID = @StudentID)
    ORDER BY    RK.ClassID, RK.SectionID, TRY_CONVERT(INT, ST.RollNo), ST.RollNo;

    /* ---- 2) Subject details (one row per student-subject) ---- */
    SELECT
        R.StudentID,
        SU.VName            AS SubjectName,
        R.VDate,
        R.TotalMarks,
        R.PassMarks,
        R.MarksObtained,
        CASE WHEN R.IsAbsent = 1 OR ISNULL(R.IsPassed, 0) = 0
             THEN 'Fail' ELSE 'Pass' END                          AS Status
    FROM        SMSExamResult R
    INNER JOIN  SMSSubject    SU ON R.SubjectID = SU.VID
    INNER JOIN  SMSStudent    ST ON R.StudentID = ST.VID
    WHERE       R.ExamID    = @ExamID
      AND       R.IsActive  = 1
      AND       (@ClassID   = 0 OR R.ClassID   = @ClassID)
      AND       (@SectionID = 0 OR R.SectionID = @SectionID)
      AND       (@StudentID = 0 OR R.StudentID = @StudentID)
      AND       EXISTS (SELECT 1 FROM SMSStudentAcademic SA
                        WHERE SA.StudentID = R.StudentID AND SA.IsActive = 1 AND SA.StatusID = 1)
    ORDER BY    R.ClassID, R.SectionID, TRY_CONVERT(INT, ST.RollNo), ST.RollNo, SU.VID;
END
GO

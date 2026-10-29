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
   whole school in one run, filtered to a single section, or to one student.

   That is now true. It used to be only half true: @SectionID was applied to
   the ranking pool as well as to the output, so printing one section ranked
   the student against that section alone. Every caller that passed a section —
   the exam screen, the result-card SMS campaign and the WhatsApp auto-reply —
   printed section-wise positions under a "class position" heading.

   Returns 2 result sets:
     (1) Student headers  -> StudentID, RollNo, StudentName, FatherName,
                             ClassName, SectionName, Position, TotalStudents,
                             HighestMarks, CurrentPct, PrevPct, GrandTotal,
                             GrandPass, GrandObt, GrandStatus,
                             PresentDays, SchoolDays, AttendancePct,
                             AcademyPresentDays, AcademyDays, AcademyPct
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

    /* ---- Per-student aggregates for the selected exam ----------------------
       NEITHER the student filter NOR the section filter is applied here, so
       each student's group stats (position, total, highest) reflect the WHOLE
       Class.

       The section filter used to be applied, and that was the bug: passing a
       section narrowed the pool the ranking runs over, so DENSE_RANK() over
       PARTITION BY ClassID was really ranking within that one section. A child
       who came 3rd in the class was printed as 1st because the other two were
       in the other section. The scope parameters belong on the OUTPUT (see the
       final SELECTs), never on the pool the position is computed from. */
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
          AND       EXISTS (SELECT 1 FROM SMSStudentAcademic SA
                            WHERE SA.StudentID = R.StudentID AND SA.IsActive = 1 AND SA.StatusID = 1)
          -- On the Result Exclude List of this exam: left out of every result report.
          AND       NOT EXISTS (SELECT 1 FROM SMSExamExclude X
                                WHERE X.ExamID = R.ExamID AND X.StudentID = R.StudentID)
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
          -- Not filtered by section either: this is joined back per student,
          -- and a child moved between sections since the last term would
          -- otherwise lose their previous percentage for no reason.
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
          -- A day whose attendance was deleted (IsActive = 0) is not counted.
          AND   ISNULL(A.IsActive, 1) = 1
        GROUP BY A.StudentID
    ),
    AcAtt AS
    (
        /* Academy attendance (SMSAcademyAttendance, AcademyAttendance.sql) in the
           same window. Only students marked there have it; the card shows the line
           for them alone. */
        SELECT  A.StudentID,
                COUNT(*)                                              AS AcademyDays,
                SUM(CASE WHEN A.Attendence = 1 THEN 1 ELSE 0 END)     AS AcademyPresentDays
        FROM    SMSAcademyAttendance A
        WHERE   (@ThisLastPaper IS NULL OR A.VDate <= @ThisLastPaper)
          AND   (@PrevLastPaper IS NULL OR A.VDate >  @PrevLastPaper)
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
                  ELSE 0 END AS DECIMAL(5,2))                      AS AttendancePct,
        ISNULL(AC.AcademyPresentDays, 0)                           AS AcademyPresentDays,
        ISNULL(AC.AcademyDays, 0)                                  AS AcademyDays,
        CAST(CASE WHEN ISNULL(AC.AcademyDays, 0) > 0
                  THEN AC.AcademyPresentDays * 100.0 / AC.AcademyDays
                  ELSE 0 END AS DECIMAL(5,2))                      AS AcademyPct
    FROM        Ranked      RK
    INNER JOIN  SMSStudent  ST ON RK.StudentID = ST.VID
    LEFT  JOIN  SMSParent   PA ON ST.ParentID  = PA.VID
    LEFT  JOIN  SMSClass    CL ON RK.ClassID   = CL.VID
    LEFT  JOIN  SMSSection  SE ON RK.SectionID = SE.VID
    LEFT  JOIN  Prev        PR ON PR.StudentID = RK.StudentID
    LEFT  JOIN  Att         AT ON AT.StudentID = RK.StudentID
    LEFT  JOIN  AcAtt       AC ON AC.StudentID = RK.StudentID
    -- Section scoping happens HERE, on the output, not in the Cur pool above.
    -- It used to live in the pool, which is what made the position section-wise;
    -- moving it here keeps "print one section" working while the ranking still
    -- runs over the whole class.
    WHERE       (@StudentID = 0 OR ST.VID       = @StudentID)
      AND       (@SectionID = 0 OR RK.SectionID = @SectionID)
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
      -- On the Result Exclude List of this exam: left out of every result report.
      AND       NOT EXISTS (SELECT 1 FROM SMSExamExclude X
                            WHERE X.ExamID = R.ExamID AND X.StudentID = R.StudentID)
    ORDER BY    R.ClassID, R.SectionID, TRY_CONVERT(INT, ST.RollNo), ST.RollNo, SU.VID;
END
GO

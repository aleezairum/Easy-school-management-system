USE [EasySchoolDB]
GO
/****** Object:  StoredProcedure [dbo].[SpGet_SMSStudentProvisionalCertificate]    Script Date: 06-May-26 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE OR ALTER PROCEDURE [dbo].[SpGet_SMSStudentProvisionalCertificate]
(
    @StudentID       INT,
    @SessionID       INT           = NULL,
    @BoardRollNo     NVARCHAR(50)  = NULL,
    @ExamRollNo      NVARCHAR(50)  = NULL,
    @Examination     NVARCHAR(200) = NULL,
    @MarksObtained   NVARCHAR(50)  = NULL,
    @TotalMarks      NVARCHAR(50)  = NULL,
    @Conduct         NVARCHAR(500) = NULL,
    @MoralCharacter  NVARCHAR(100) = NULL,
    @IssueDate       DATETIME      = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    -- If a specific session was requested, scope the exam lookup to the
    -- Class/Section the student was enrolled in during that session (and pick
    -- up their roll number for that session); a student's exam results carry
    -- ClassID/SectionID but not SessionID.
    DECLARE @SessionClassID   INT = NULL;
    DECLARE @SessionSectionID INT = NULL;
    DECLARE @ClassRollNo      NVARCHAR(50) = NULL;
    IF @SessionID IS NOT NULL AND @SessionID <> 0
    BEGIN
        SELECT TOP 1 @SessionClassID = Sec.ClassID, @SessionSectionID = A.SectionID, @ClassRollNo = A.RollNo
        FROM   SMSStudentAcademic A
        INNER JOIN SMSSection     Sec ON A.SectionID = Sec.VID
        WHERE  A.StudentID = @StudentID AND A.SessionID = @SessionID
        ORDER BY A.VID DESC;
    END
    ELSE
    BEGIN
        -- No session specified: fall back to the student's latest active
        -- academic record, same one SessionName below resolves to.
        SELECT TOP 1 @ClassRollNo = A.RollNo
        FROM   SMSStudentAcademic A
        WHERE  A.StudentID = @StudentID AND A.StatusID = 1
        ORDER BY A.VID DESC;
    END

    -- Latest exam the student has results for (most recent result date, then
    -- highest ExamID as a tiebreak), restricted to the requested session's
    -- Class/Section when one was given.
    DECLARE @LatestExamID INT = (
        SELECT TOP 1 R.ExamID
        FROM   SMSExamResult R
        WHERE  R.StudentID = @StudentID AND R.IsActive = 1
          AND  (@SessionClassID IS NULL OR (R.ClassID = @SessionClassID AND R.SectionID = @SessionSectionID))
        ORDER BY R.VDate DESC, R.ExamID DESC
    );

    DECLARE @ExamTotalMarks    DECIMAL(9,2);
    DECLARE @ExamObtainedMarks DECIMAL(9,2);

    SELECT
        @ExamTotalMarks    = SUM(CASE WHEN R.IsAbsent = 0 THEN ISNULL(R.TotalMarks, 0)    ELSE 0 END),
        @ExamObtainedMarks = SUM(CASE WHEN R.IsAbsent = 0 THEN ISNULL(R.MarksObtained, 0) ELSE 0 END)
    FROM   SMSExamResult R
    WHERE  R.StudentID = @StudentID AND R.ExamID = @LatestExamID AND R.IsActive = 1
      AND  (@SessionClassID IS NULL OR (R.ClassID = @SessionClassID AND R.SectionID = @SessionSectionID));

    DECLARE @ExamName NVARCHAR(200) = (SELECT VName FROM SMSExam WHERE VID = @LatestExamID);

    SELECT
        -- SMSStudent Information
        S.VID,
        S.CampusID,
        S.RegistrationNo,
        S.AdmissionNo,
        S.AdmissionDate,
        S.RollNo,
        S.Name        AS StudentName,
        S.Name_Urdu,
        S.DOB,
        S.Gender,
        S.Address,

        -- SMSParent Information
        ISNULL(P.FatherName, '')      AS FatherName,
        ISNULL(P.FatherNameUrdu, '')  AS FatherNameUrdu,
        ISNULL(P.FatherCNIC, '')      AS FatherCNIC,

        -- Session (specific session if @SessionID passed, otherwise latest)
        ISNULL((
            SELECT TOP 1 SY.VName
            FROM   SMSStudentAcademic     A
            INNER JOIN AcademicSessionYear SY ON A.SessionID = SY.VID
            WHERE  A.StudentID = S.VID
              AND  (@SessionID IS NULL OR @SessionID = 0 OR A.SessionID = @SessionID)
            ORDER BY SY.VID DESC
        ), '') AS SessionName,

        -- Certificate fields (parameters with sensible defaults)
        ISNULL(@BoardRollNo,    '')     AS BoardRollNo,
        COALESCE(NULLIF(@ExamRollNo, ''),    @ClassRollNo,                          '') AS ExamRollNo,
        COALESCE(NULLIF(@Examination, ''),   @ExamName,                             'N.A.') AS Examination,
        COALESCE(NULLIF(@MarksObtained, ''), CAST(@ExamObtainedMarks AS NVARCHAR(50)), 'N.A.') AS MarksObtained,
        COALESCE(NULLIF(@TotalMarks, ''),    CAST(@ExamTotalMarks AS NVARCHAR(50)),    'N.A.') AS TotalMarks,
        ISNULL(@Conduct,        'regular, punctual, obedient and hard working') AS Conduct,
        ISNULL(@MoralCharacter, 'Excellent') AS MoralCharacter,
        ISNULL(@IssueDate, GETDATE())       AS IssueDate

    FROM       SMSStudent  S
    LEFT OUTER JOIN SMSParent P ON S.ParentID = P.VID
    WHERE      S.VID = @StudentID
      AND      S.IsActive = 1;
END
GO
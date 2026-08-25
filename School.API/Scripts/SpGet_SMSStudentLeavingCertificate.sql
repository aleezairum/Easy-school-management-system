USE [EasySchoolDB]
GO
/****** Object:  StoredProcedure [dbo].[SpGet_SMSStudentLeavingCertificate]    Script Date: 06-May-26 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE OR ALTER PROCEDURE [dbo].[SpGet_SMSStudentLeavingCertificate]
(
    @StudentID     INT,
    @LeavingDate   DATETIME      = NULL,
    @Examination   NVARCHAR(100) = NULL,
    @MarksObtained NVARCHAR(50)  = NULL,
    @TotalMarks    NVARCHAR(50)  = NULL,
    @ClassAdmitted NVARCHAR(100) = NULL,
    @ClassStudying NVARCHAR(100) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    -- Latest exam the student has results for (most recent result date, then
    -- highest ExamID as a tiebreak) — this is the exam whose marks/result
    -- populate the certificate unless the caller overrides them.
    DECLARE @LatestExamID INT = (
        SELECT TOP 1 R.ExamID
        FROM   SMSExamResult R
        WHERE  R.StudentID = @StudentID AND R.IsActive = 1
        ORDER BY R.VDate DESC, R.ExamID DESC
    );

    DECLARE @ExamTotalMarks    DECIMAL(9,2);
    DECLARE @ExamObtainedMarks DECIMAL(9,2);
    DECLARE @ExamAppeared      INT;
    DECLARE @ExamFailed        INT;

    SELECT
        @ExamTotalMarks    = SUM(CASE WHEN R.IsAbsent = 0 THEN ISNULL(R.TotalMarks, 0)    ELSE 0 END),
        @ExamObtainedMarks = SUM(CASE WHEN R.IsAbsent = 0 THEN ISNULL(R.MarksObtained, 0) ELSE 0 END),
        @ExamAppeared      = SUM(CASE WHEN R.IsAbsent = 0 THEN 1 ELSE 0 END),
        @ExamFailed        = SUM(CASE WHEN R.IsAbsent = 0 AND ISNULL(R.IsPassed, 0) = 0 THEN 1 ELSE 0 END)
    FROM   SMSExamResult R
    WHERE  R.StudentID = @StudentID AND R.ExamID = @LatestExamID AND R.IsActive = 1;

    DECLARE @ExamName   NVARCHAR(100) = (SELECT VName FROM SMSExam WHERE VID = @LatestExamID);
    DECLARE @ResultText NVARCHAR(10) = CASE
        WHEN @ExamAppeared IS NULL OR @ExamAppeared = 0 THEN ''
        WHEN @ExamFailed > 0                             THEN 'Failed'
        ELSE 'Passed'
    END;

    SELECT
        -- SMSStudent Information
        S.VID,
        S.CampusID,
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
        ISNULL(P.FatherMobile, '')    AS FatherMobile,

        -- Class in which admitted (earliest academic record, unless overridden)
        COALESCE(NULLIF(@ClassAdmitted, ''), (
            SELECT TOP 1 C1.VName
            FROM   SMSStudentAcademic     A1
            INNER JOIN SMSSection         Sec1 ON A1.SectionID = Sec1.VID
            INNER JOIN SMSClass           C1   ON Sec1.ClassID = C1.VID
            INNER JOIN AcademicSessionYear SY1 ON A1.SessionID = SY1.VID
            WHERE  A1.StudentID = S.VID
            ORDER BY SY1.VID ASC
        ), '') AS ClassAdmitted,

        -- Class currently studying / Passed / Failed (latest academic record +
        -- latest exam result, unless overridden)
        COALESCE(NULLIF(@ClassStudying, ''), (
            SELECT TOP 1
                C2.VName + CASE WHEN @ResultText <> '' THEN ' / ' + @ResultText ELSE '' END
            FROM   SMSStudentAcademic     A2
            INNER JOIN SMSSection         Sec2 ON A2.SectionID = Sec2.VID
            INNER JOIN SMSClass           C2   ON Sec2.ClassID = C2.VID
            INNER JOIN AcademicSessionYear SY2 ON A2.SessionID = SY2.VID
            WHERE  A2.StudentID = S.VID
            ORDER BY SY2.VID DESC
        ), '') AS ClassStudying,

        -- Leaving / Examination Information (from the latest exam result,
        -- unless overridden by the caller)
        ISNULL(@LeavingDate, GETDATE())                                                  AS LeavingDate,
        COALESCE(NULLIF(@Examination, ''),   @ExamName,                             'N.A.') AS Examination,
        COALESCE(NULLIF(@MarksObtained, ''), CAST(@ExamObtainedMarks AS NVARCHAR(50)), 'N.A.') AS MarksObtained,
        COALESCE(NULLIF(@TotalMarks, ''),    CAST(@ExamTotalMarks AS NVARCHAR(50)),    'N.A.') AS TotalMarks

    FROM       SMSStudent  S
    LEFT OUTER JOIN SMSParent P ON S.ParentID = P.VID
    WHERE      S.VID = @StudentID
      AND      S.IsActive = 1;
END
GO
USE [EasySchoolDB]
GO
/****** Object:  StoredProcedure [dbo].[SpGet_SMSStudentProvisionalCertificate]    Script Date: 06-May-26 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[SpGet_SMSStudentProvisionalCertificate]
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
        ISNULL(@ExamRollNo,     '')     AS ExamRollNo,
        ISNULL(@Examination,    'N.A.') AS Examination,
        ISNULL(@MarksObtained,  'N.A.') AS MarksObtained,
        ISNULL(@TotalMarks,     'N.A.') AS TotalMarks,
        ISNULL(@Conduct,        'regular, punctual, obedient and hard working') AS Conduct,
        ISNULL(@MoralCharacter, 'Excellent') AS MoralCharacter,
        @IssueDate                          AS IssueDate

    FROM       SMSStudent  S
    LEFT OUTER JOIN SMSParent P ON S.ParentID = P.VID
    WHERE      S.VID = @StudentID
      AND      S.IsActive = 1;
END
GO
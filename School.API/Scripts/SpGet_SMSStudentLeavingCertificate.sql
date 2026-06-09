USE [EasySchoolDB]
GO
/****** Object:  StoredProcedure [dbo].[SpGet_SMSStudentLeavingCertificate]    Script Date: 06-May-26 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[SpGet_SMSStudentLeavingCertificate]
(
    @StudentID     INT,
    @LeavingDate   DATETIME      = NULL,
    @Examination   NVARCHAR(100) = NULL,
    @MarksObtained NVARCHAR(50)  = NULL,
    @TotalMarks    NVARCHAR(50)  = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

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

        -- Class in which admitted (earliest academic record)
        ISNULL((
            SELECT TOP 1 C1.VName
            FROM   SMSStudentAcademic     A1
            INNER JOIN SMSSection         Sec1 ON A1.SectionID = Sec1.VID
            INNER JOIN SMSClass           C1   ON Sec1.ClassID = C1.VID
            INNER JOIN AcademicSessionYear SY1 ON A1.SessionID = SY1.VID
            WHERE  A1.StudentID = S.VID
            ORDER BY SY1.VID ASC
        ), '') AS ClassAdmitted,

        -- Class currently studying / Passed / Failed (latest academic record)
        ISNULL((
            SELECT TOP 1 C2.VName
            FROM   SMSStudentAcademic     A2
            INNER JOIN SMSSection         Sec2 ON A2.SectionID = Sec2.VID
            INNER JOIN SMSClass           C2   ON Sec2.ClassID = C2.VID
            INNER JOIN AcademicSessionYear SY2 ON A2.SessionID = SY2.VID
            WHERE  A2.StudentID = S.VID
            ORDER BY SY2.VID DESC
        ), '') AS ClassStudying,

        -- Leaving / Examination Information (passed as parameters)
        ISNULL(@LeavingDate, GETDATE()) AS LeavingDate,
        ISNULL(@Examination,   'N.A.')  AS Examination,
        ISNULL(@MarksObtained, 'N.A.')  AS MarksObtained,
        ISNULL(@TotalMarks,    'N.A.')  AS TotalMarks

    FROM       SMSStudent  S
    LEFT OUTER JOIN SMSParent P ON S.ParentID = P.VID
    WHERE      S.VID = @StudentID
      AND      S.IsActive = 1;
END
GO
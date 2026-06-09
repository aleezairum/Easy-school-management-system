USE [EasySchoolDB]
GO
/****** Object: StoredProcedure [dbo].[SpRPT_SMSStudentFee] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[SpRPT_SMSStudentFee]
(
    @VID        INT = NULL,
    @StudentID  INT = NULL,
    @SessionID  INT = NULL,
    @SectionIDs NVARCHAR(MAX) = NULL,   -- comma separated
    @ClassIDs   NVARCHAR(MAX) = NULL,   -- comma separated
    @FeeTypeIDs NVARCHAR(MAX) = NULL,   -- comma separated
    @DateFrom   DATETIME,
    @DateTo     DATETIME,
    @ReportType INT   -- 0 = Paid, 1 = UnPaid, 2 = All
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @ReportType = 1
    BEGIN
        -- ===== UNPAID =====
        SELECT F.VID, F.VDate, S.RollNo, S.Name AS StudentName, P.FatherName, P.FatherMobile AS ContactNo,
               C.VName AS ClassName, SY.VName AS SessionName, SEC.VName AS SectionName, G.VName AS GradeName,
               FT.VName AS FeeTypeName, F.FeeAmount, F.ClearedAmount, F.FeeAmount - F.ClearedAmount AS PendingAmount,
               F.Remarks, F.IsClear
        FROM SMSStudentFee F
        LEFT JOIN SMSStudent          S   ON F.StudentID = S.VID
        LEFT JOIN SMSStudentAcademic  SA  ON SA.StudentID = S.VID
        LEFT JOIN SMSParent           P   ON S.ParentID  = P.VID
        LEFT JOIN AcademicSessionYear SY  ON F.SessionID = SY.VID
        LEFT JOIN SMSSection          SEC ON F.SectionID = SEC.VID
        LEFT JOIN SMSClass            C   ON SEC.ClassID = C.VID
        LEFT JOIN AcademicGrades      G   ON F.GradeID   = G.VID
        LEFT JOIN SMSFeeType          FT  ON F.FeeTypeID = FT.VID
        WHERE
            SA.StatusID = 1
            AND (
                    (@ReportType = 1 AND F.FeeAmount - F.ClearedAmount > 0)
                    AND
                    (@ReportType = 1 AND F.VDate BETWEEN @DateFrom AND @DateTo)
                )
            AND F.VDate < GETDATE()

            -- Basic Filters
            AND (@VID       IS NULL OR @VID       = 0 OR F.VID       = @VID)
            AND (@StudentID IS NULL OR @StudentID = 0 OR F.StudentID = @StudentID)
            AND (@SessionID IS NULL OR @SessionID = 0 OR F.SessionID = @SessionID)

            -- Section Filter
            AND (@SectionIDs IS NULL OR @SectionIDs = '' OR F.SectionID IN (SELECT Value FROM dbo.SplitString(@SectionIDs, ',')))
            -- Class Filter
            AND (@ClassIDs   IS NULL OR @ClassIDs   = '' OR SEC.ClassID IN (SELECT Value FROM dbo.SplitString(@ClassIDs, ',')))
            -- FeeType Filter
            AND (@FeeTypeIDs IS NULL OR @FeeTypeIDs = '' OR F.FeeTypeID IN (SELECT Value FROM dbo.SplitString(@FeeTypeIDs, ',')))

            AND F.IsActive = 1
        ORDER BY F.VDate DESC;
    END
    ELSE IF @ReportType = 0
    BEGIN
        -- ===== PAID (from vouchers) =====
        SELECT F.VID, F.VDate, S.RollNo, S.Name AS StudentName, P.FatherName, P.FatherMobile AS ContactNo,
               C.VName AS ClassName, SY.VName AS SessionName, SEC.VName AS SectionName, G.VName AS GradeName,
               FT.VName AS FeeTypeName, D.PaidAmount FeeAmount, D.PaidAmount ClearedAmount, D.PendingBalance AS PendingAmount,
               F.Remarks, F.IsClear
        FROM SMSStudentFee F
        INNER JOIN FeeVoucherDetail   D   ON D.StudentFeeID = F.VID
        INNER JOIN FeeVoucherMain     H   ON H.VID = D.MID
        LEFT JOIN SMSStudent          S   ON F.StudentID = S.VID
        LEFT JOIN SMSStudentAcademic  SA  ON SA.StudentID = S.VID
        LEFT JOIN SMSParent           P   ON S.ParentID  = P.VID
        LEFT JOIN AcademicSessionYear SY  ON F.SessionID = SY.VID
        LEFT JOIN SMSSection          SEC ON F.SectionID = SEC.VID
        LEFT JOIN SMSClass            C   ON SEC.ClassID = C.VID
        LEFT JOIN AcademicGrades      G   ON F.GradeID   = G.VID
        LEFT JOIN SMSFeeType          FT  ON F.FeeTypeID = FT.VID
        WHERE
            (
                (@ReportType = 1 AND F.FeeAmount - F.ClearedAmount > 0)
                OR
                (@ReportType <> 1 AND H.VDate BETWEEN @DateFrom AND @DateTo)
            )

            -- Basic Filters
            AND (@VID       IS NULL OR @VID       = 0 OR F.VID       = @VID)
            AND (@StudentID IS NULL OR @StudentID = 0 OR F.StudentID = @StudentID)
            AND (@SessionID IS NULL OR @SessionID = 0 OR F.SessionID = @SessionID)

            -- Section Filter
            AND (@SectionIDs IS NULL OR @SectionIDs = '' OR F.SectionID IN (SELECT Value FROM dbo.SplitString(@SectionIDs, ',')))
            -- Class Filter
            AND (@ClassIDs   IS NULL OR @ClassIDs   = '' OR SEC.ClassID IN (SELECT Value FROM dbo.SplitString(@ClassIDs, ',')))
            -- FeeType Filter
            AND (@FeeTypeIDs IS NULL OR @FeeTypeIDs = '' OR F.FeeTypeID IN (SELECT Value FROM dbo.SplitString(@FeeTypeIDs, ',')))

            AND F.IsActive = 1
        ORDER BY F.VDate DESC;
    END
    ELSE
    BEGIN
        -- ===== ALL (pending rows UNION paid rows) =====
        SELECT F.VID, F.VDate, S.RollNo, S.Name AS StudentName, P.FatherName, P.FatherMobile AS ContactNo,
               C.VName AS ClassName, SY.VName AS SessionName, SEC.VName AS SectionName, G.VName AS GradeName,
               FT.VName AS FeeTypeName, 0 FeeAmount, 0 ClearedAmount, F.FeeAmount - F.ClearedAmount AS PendingAmount,
               F.Remarks, F.IsClear
        FROM SMSStudentFee F
        LEFT JOIN SMSStudent          S   ON F.StudentID = S.VID
        LEFT JOIN SMSStudentAcademic  SA  ON SA.StudentID = S.VID
        LEFT JOIN SMSParent           P   ON S.ParentID  = P.VID
        LEFT JOIN AcademicSessionYear SY  ON F.SessionID = SY.VID
        LEFT JOIN SMSSection          SEC ON F.SectionID = SEC.VID
        LEFT JOIN SMSClass            C   ON SEC.ClassID = C.VID
        LEFT JOIN AcademicGrades      G   ON F.GradeID   = G.VID
        LEFT JOIN SMSFeeType          FT  ON F.FeeTypeID = FT.VID
        WHERE
            SA.StatusID = 1
            AND F.VDate BETWEEN @DateFrom AND @DateTo
            AND F.VDate < GETDATE()

            -- Basic Filters
            AND (@VID       IS NULL OR @VID       = 0 OR F.VID       = @VID)
            AND (@StudentID IS NULL OR @StudentID = 0 OR F.StudentID = @StudentID)
            AND (@SessionID IS NULL OR @SessionID = 0 OR F.SessionID = @SessionID)

            -- Section Filter
            AND (@SectionIDs IS NULL OR @SectionIDs = '' OR F.SectionID IN (SELECT Value FROM dbo.SplitString(@SectionIDs, ',')))
            -- Class Filter
            AND (@ClassIDs   IS NULL OR @ClassIDs   = '' OR SEC.ClassID IN (SELECT Value FROM dbo.SplitString(@ClassIDs, ',')))
            -- FeeType Filter
            AND (@FeeTypeIDs IS NULL OR @FeeTypeIDs = '' OR F.FeeTypeID IN (SELECT Value FROM dbo.SplitString(@FeeTypeIDs, ',')))

            AND F.IsActive = 1

        UNION ALL

        SELECT F.VID, F.VDate, S.RollNo, S.Name AS StudentName, P.FatherName, P.FatherMobile AS ContactNo,
               C.VName AS ClassName, SY.VName AS SessionName, SEC.VName AS SectionName, G.VName AS GradeName,
               FT.VName AS FeeTypeName, D.PaidAmount FeeAmount, D.PaidAmount ClearedAmount, 0 AS PendingAmount,
               F.Remarks, F.IsClear
        FROM SMSStudentFee F
        INNER JOIN FeeVoucherDetail   D   ON D.StudentFeeID = F.VID
        INNER JOIN FeeVoucherMain     H   ON H.VID = D.MID
        LEFT JOIN SMSStudent          S   ON F.StudentID = S.VID
        LEFT JOIN SMSStudentAcademic  SA  ON SA.StudentID = S.VID
        LEFT JOIN SMSParent           P   ON S.ParentID  = P.VID
        LEFT JOIN AcademicSessionYear SY  ON F.SessionID = SY.VID
        LEFT JOIN SMSSection          SEC ON F.SectionID = SEC.VID
        LEFT JOIN SMSClass            C   ON SEC.ClassID = C.VID
        LEFT JOIN AcademicGrades      G   ON F.GradeID   = G.VID
        LEFT JOIN SMSFeeType          FT  ON F.FeeTypeID = FT.VID
        WHERE
            (
                (@ReportType = 1 AND F.FeeAmount - F.ClearedAmount > 0)
                OR
                (@ReportType <> 1 AND H.VDate BETWEEN @DateFrom AND @DateTo)
            )

            -- Basic Filters
            AND (@VID       IS NULL OR @VID       = 0 OR F.VID       = @VID)
            AND (@StudentID IS NULL OR @StudentID = 0 OR F.StudentID = @StudentID)
            AND (@SessionID IS NULL OR @SessionID = 0 OR F.SessionID = @SessionID)

            -- Section Filter
            AND (@SectionIDs IS NULL OR @SectionIDs = '' OR F.SectionID IN (SELECT Value FROM dbo.SplitString(@SectionIDs, ',')))
            -- Class Filter
            AND (@ClassIDs   IS NULL OR @ClassIDs   = '' OR SEC.ClassID IN (SELECT Value FROM dbo.SplitString(@ClassIDs, ',')))
            -- FeeType Filter
            AND (@FeeTypeIDs IS NULL OR @FeeTypeIDs = '' OR F.FeeTypeID IN (SELECT Value FROM dbo.SplitString(@FeeTypeIDs, ',')))

            AND F.IsActive = 1
        ORDER BY F.VDate DESC;
    END
END
GO

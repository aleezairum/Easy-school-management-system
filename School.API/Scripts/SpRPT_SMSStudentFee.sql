/* ============================================================================
   SpRPT_SMSStudentFee — the Fee Report behind /fee-report
   ============================================================================

   Deliberately NO "USE [EasySchoolDB]" here. A hard-coded USE overrides
   sqlcmd's -d and sends the script to whatever that name resolves to on the
   machine it runs on — which is how a script "succeeds" while quietly building
   its objects in the development database. The database comes from -d instead.

   Run with:
     cd School.API\Scripts
     sqlcmd -S . -U sa -P <pwd> -d EasySchoolDB -i SpRPT_SMSStudentFee.sql
   ============================================================================ */
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Create a stub if it is missing, so the ALTER below works on a fresh database
-- as well as on one that already has the procedure.
IF OBJECT_ID('dbo.SpRPT_SMSStudentFee', 'P') IS NULL
    EXEC('CREATE PROCEDURE dbo.SpRPT_SMSStudentFee AS SET NOCOUNT ON;');
GO

ALTER PROCEDURE [dbo].[SpRPT_SMSStudentFee]
(
    @VID        INT = NULL,
    @StudentID  INT = NULL,
    @SessionID  INT = NULL,
    @SectionIDs NVARCHAR(MAX) = NULL,   -- comma separated
    @ClassIDs   NVARCHAR(MAX) = NULL,   -- comma separated
    @FeeTypeIDs NVARCHAR(MAX) = NULL,   -- comma separated
    @CampusIDs  NVARCHAR(MAX) = NULL,   -- comma separated; empty = every campus

    /* Students whose parents have promised to pay by a date that has not yet
       passed are left OFF the unpaid list — that is the whole point of taking
       the promise. Set this to 1 to see them anyway, which is the only way to
       get a true total of everything owed. See vwActiveFeePromise. */
    @IncludePromised BIT = 0,
    @DateFrom   DATETIME,
    @DateTo     DATETIME,
    -- 0 = Paid, 1 = UnPaid, 2 = All, 3 = UnPaid for students who have LEFT
    @ReportType INT
)
AS
BEGIN
    SET NOCOUNT ON;

    /* The application connects with ARITHABORT OFF, which SQL Server treats as
       a DIFFERENT plan from the one sqlcmd and Management Studio compile. This
       report is fragile enough that the two plans are not remotely alike: the
       same call ran in 55 ms under ARITHABORT ON and 32 SECONDS under OFF -
       past the 30-second command timeout, so the screen answered 500 while the
       same query pasted into SSMS looked instant.

       Setting it here pins the good one for every caller. The report does no
       division, so the arithmetic behaviour this also switches on cannot bite.
       There is no divide, no overflow-prone maths - only subtraction of two
       money columns. */
    SET ARITHABORT ON;

    /* The promises in force, pulled ONCE into a temp table.

       Joining the view straight into these queries cost 45 seconds on 380
       rows - past SQL's own 30-second command timeout, so the report simply
       failed. The view is 26 ms on its own; it is the optimiser folding a
       windowed self-join into a five-way report query that falls apart.
       Materialising it first is both correct and instant, and it is read in
       two places below, so it is only built once either way. */
    CREATE TABLE #Promise
    (
        StudentID       INT PRIMARY KEY,
        PromiseDate     DATE,
        PromisedBy      NVARCHAR(200),
        PromisedAmount  DECIMAL(18,2),
        PromisedForName NVARCHAR(200)
    );

    INSERT #Promise (StudentID, PromiseDate, PromisedBy, PromisedAmount, PromisedForName)
    SELECT StudentID, PromiseDate, PromisedBy, PromisedAmount, PromisedForName
    FROM   dbo.vwActiveFeePromiseDetail;

    IF @ReportType = 1
    BEGIN
        -- ===== UNPAID =====
        SELECT F.VID, F.VDate, S.RollNo, S.Name AS StudentName, P.FatherName, P.FatherMobile AS ContactNo,
               C.VID AS ClassID, C.VName AS ClassName, SEC.VID AS SectionID,
               SY.VName AS SessionName, SEC.VName AS SectionName, G.VName AS GradeName,
               CP.VName AS CampusName, ST.VName AS StatusName, FT.VName AS FeeTypeName, F.FeeAmount, F.ClearedAmount, F.FeeAmount - F.ClearedAmount AS PendingAmount,
               F.Remarks, F.IsClear,
               PRM.PromiseDate, PRM.PromisedBy, PRM.PromisedAmount, PRM.PromisedForName
        FROM SMSStudentFee F
        LEFT JOIN SMSStudent          S   ON F.StudentID = S.VID
        LEFT JOIN SMSStudentAcademic  SA  ON SA.StudentID = S.VID
        LEFT JOIN SMSParent           P   ON S.ParentID  = P.VID
        LEFT JOIN AcademicSessionYear SY  ON F.SessionID = SY.VID
        LEFT JOIN SMSSection          SEC ON F.SectionID = SEC.VID
        LEFT JOIN SMSClass            C   ON SEC.ClassID = C.VID
        LEFT JOIN AcademicGrades      G   ON F.GradeID   = G.VID
        LEFT JOIN SMSFeeType          FT  ON F.FeeTypeID = FT.VID
        LEFT JOIN SMSStudentStatus    ST  ON ST.VID       = SA.StatusID
        LEFT JOIN SMSCampus           CP  ON CP.VID       = S.CampusID
        -- The promise, if this family has one in force. Printed beside the
        -- student so a sheet showing a promised family says on its face why
        -- nobody has chased them and when the money is expected.
        LEFT JOIN #Promise PRM ON PRM.StudentID = F.StudentID
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
            -- Campus Filter. On the STUDENT, because a fee row carries no campus.
            AND (@CampusIDs  IS NULL OR @CampusIDs  = '' OR S.CampusID  IN (SELECT Value FROM dbo.SplitString(@CampusIDs, ',')))

            -- A promise to pay, still in force, keeps the student off this list.
            AND (@IncludePromised = 1 OR NOT EXISTS (SELECT 1 FROM #Promise HOLD WHERE HOLD.StudentID = F.StudentID))

            AND F.IsActive = 1
        ORDER BY F.VDate DESC;
    END
    ELSE IF @ReportType = 3
    BEGIN
        /* ===== UNPAID — students who have LEFT ==========================
           The same query as UNPAID above with the status test inverted.

           It exists because every other branch here filters SA.StatusID = 1,
           so the moment a child is marked Dropped Out their unpaid fee
           disappears from every fee report the school has — which is exactly
           the money most worth chasing.

           "Left" is anything that is not Active rather than Dropped Out alone:
           a Passed Out student who still owes fees is the same debt and would
           otherwise stay invisible for the same reason. StatusName is on every
           row so it is always clear which kind of leaver it is.

           NOT restricted to the date range on purpose — a leaver's arrears are
           whatever is outstanding today, and dating the report to this month
           would report nothing at all. The range still applies to the other
           three report types. */
        SELECT F.VID, F.VDate, S.RollNo, S.Name AS StudentName, P.FatherName, P.FatherMobile AS ContactNo,
               C.VID AS ClassID, C.VName AS ClassName, SEC.VID AS SectionID,
               SY.VName AS SessionName, SEC.VName AS SectionName, G.VName AS GradeName,
               CP.VName AS CampusName, ST.VName AS StatusName, FT.VName AS FeeTypeName, F.FeeAmount, F.ClearedAmount,
               F.FeeAmount - F.ClearedAmount AS PendingAmount,
               F.Remarks, F.IsClear,
               PRM.PromiseDate, PRM.PromisedBy, PRM.PromisedAmount, PRM.PromisedForName
        FROM SMSStudentFee F
        LEFT JOIN SMSStudent          S   ON F.StudentID = S.VID
        LEFT JOIN SMSStudentAcademic  SA  ON SA.StudentID = S.VID
        LEFT JOIN SMSParent           P   ON S.ParentID  = P.VID
        LEFT JOIN AcademicSessionYear SY  ON F.SessionID = SY.VID
        LEFT JOIN SMSSection          SEC ON F.SectionID = SEC.VID
        LEFT JOIN SMSClass            C   ON SEC.ClassID = C.VID
        LEFT JOIN AcademicGrades      G   ON F.GradeID   = G.VID
        LEFT JOIN SMSFeeType          FT  ON F.FeeTypeID = FT.VID
        LEFT JOIN SMSStudentStatus    ST  ON ST.VID       = SA.StatusID
        LEFT JOIN SMSCampus           CP  ON CP.VID       = S.CampusID
        -- The promise, if this family has one in force. Printed beside the
        -- student so a sheet showing a promised family says on its face why
        -- nobody has chased them and when the money is expected.
        LEFT JOIN #Promise PRM ON PRM.StudentID = F.StudentID
        WHERE
            SA.StatusID IS NOT NULL
            AND SA.StatusID <> 1
            AND F.FeeAmount - F.ClearedAmount > 0
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
            -- Campus Filter. On the STUDENT, because a fee row carries no campus.
            AND (@CampusIDs  IS NULL OR @CampusIDs  = '' OR S.CampusID  IN (SELECT Value FROM dbo.SplitString(@CampusIDs, ',')))

            -- A promise to pay, still in force, keeps the student off this list.
            AND (@IncludePromised = 1 OR NOT EXISTS (SELECT 1 FROM #Promise HOLD WHERE HOLD.StudentID = F.StudentID))

            AND F.IsActive = 1
        ORDER BY S.Name, F.VDate;
    END
    ELSE IF @ReportType = 0
    BEGIN
        -- ===== PAID (from vouchers) =====
        SELECT F.VID, F.VDate, S.RollNo, S.Name AS StudentName, P.FatherName, P.FatherMobile AS ContactNo,
               C.VID AS ClassID, C.VName AS ClassName, SEC.VID AS SectionID,
               SY.VName AS SessionName, SEC.VName AS SectionName, G.VName AS GradeName,
               CP.VName AS CampusName, ST.VName AS StatusName, FT.VName AS FeeTypeName, D.PaidAmount FeeAmount, D.PaidAmount ClearedAmount, D.PendingBalance AS PendingAmount,
               F.Remarks, F.IsClear,
               PRM.PromiseDate, PRM.PromisedBy, PRM.PromisedAmount, PRM.PromisedForName
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
        LEFT JOIN SMSStudentStatus    ST  ON ST.VID       = SA.StatusID
        LEFT JOIN SMSCampus           CP  ON CP.VID       = S.CampusID
        -- The promise, if this family has one in force. Printed beside the
        -- student so a sheet showing a promised family says on its face why
        -- nobody has chased them and when the money is expected.
        LEFT JOIN #Promise PRM ON PRM.StudentID = F.StudentID
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
            -- Campus Filter. On the STUDENT, because a fee row carries no campus.
            AND (@CampusIDs  IS NULL OR @CampusIDs  = '' OR S.CampusID  IN (SELECT Value FROM dbo.SplitString(@CampusIDs, ',')))

            -- No promise filter here: this side lists money actually RECEIVED,
            -- and hiding a receipt because the family owes something else would
            -- understate what the school took.

            AND F.IsActive = 1
        ORDER BY F.VDate DESC;
    END
    ELSE
    BEGIN
        -- ===== ALL (pending rows UNION paid rows) =====
        SELECT F.VID, F.VDate, S.RollNo, S.Name AS StudentName, P.FatherName, P.FatherMobile AS ContactNo,
               C.VID AS ClassID, C.VName AS ClassName, SEC.VID AS SectionID,
               SY.VName AS SessionName, SEC.VName AS SectionName, G.VName AS GradeName,
               CP.VName AS CampusName, ST.VName AS StatusName, FT.VName AS FeeTypeName, 0 FeeAmount, 0 ClearedAmount, F.FeeAmount - F.ClearedAmount AS PendingAmount,
               F.Remarks, F.IsClear,
               PRM.PromiseDate, PRM.PromisedBy, PRM.PromisedAmount, PRM.PromisedForName
        FROM SMSStudentFee F
        LEFT JOIN SMSStudent          S   ON F.StudentID = S.VID
        LEFT JOIN SMSStudentAcademic  SA  ON SA.StudentID = S.VID
        LEFT JOIN SMSParent           P   ON S.ParentID  = P.VID
        LEFT JOIN AcademicSessionYear SY  ON F.SessionID = SY.VID
        LEFT JOIN SMSSection          SEC ON F.SectionID = SEC.VID
        LEFT JOIN SMSClass            C   ON SEC.ClassID = C.VID
        LEFT JOIN AcademicGrades      G   ON F.GradeID   = G.VID
        LEFT JOIN SMSFeeType          FT  ON F.FeeTypeID = FT.VID
        LEFT JOIN SMSStudentStatus    ST  ON ST.VID       = SA.StatusID
        LEFT JOIN SMSCampus           CP  ON CP.VID       = S.CampusID
        -- The promise, if this family has one in force. Printed beside the
        -- student so a sheet showing a promised family says on its face why
        -- nobody has chased them and when the money is expected.
        LEFT JOIN #Promise PRM ON PRM.StudentID = F.StudentID
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
            -- Campus Filter. On the STUDENT, because a fee row carries no campus.
            AND (@CampusIDs  IS NULL OR @CampusIDs  = '' OR S.CampusID  IN (SELECT Value FROM dbo.SplitString(@CampusIDs, ',')))

            -- A promise to pay, still in force, keeps the student off this list.
            AND (@IncludePromised = 1 OR NOT EXISTS (SELECT 1 FROM #Promise HOLD WHERE HOLD.StudentID = F.StudentID))

            AND F.IsActive = 1

        UNION ALL

        SELECT F.VID, F.VDate, S.RollNo, S.Name AS StudentName, P.FatherName, P.FatherMobile AS ContactNo,
               C.VID AS ClassID, C.VName AS ClassName, SEC.VID AS SectionID,
               SY.VName AS SessionName, SEC.VName AS SectionName, G.VName AS GradeName,
               CP.VName AS CampusName, ST.VName AS StatusName, FT.VName AS FeeTypeName, D.PaidAmount FeeAmount, D.PaidAmount ClearedAmount, 0 AS PendingAmount,
               F.Remarks, F.IsClear,
               PRM.PromiseDate, PRM.PromisedBy, PRM.PromisedAmount, PRM.PromisedForName
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
        LEFT JOIN SMSStudentStatus    ST  ON ST.VID       = SA.StatusID
        LEFT JOIN SMSCampus           CP  ON CP.VID       = S.CampusID
        -- The promise, if this family has one in force. Printed beside the
        -- student so a sheet showing a promised family says on its face why
        -- nobody has chased them and when the money is expected.
        LEFT JOIN #Promise PRM ON PRM.StudentID = F.StudentID
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
            -- Campus Filter. On the STUDENT, because a fee row carries no campus.
            AND (@CampusIDs  IS NULL OR @CampusIDs  = '' OR S.CampusID  IN (SELECT Value FROM dbo.SplitString(@CampusIDs, ',')))

            -- No promise filter here: this side lists money actually RECEIVED,
            -- and hiding a receipt because the family owes something else would
            -- understate what the school took.

            AND F.IsActive = 1
        ORDER BY F.VDate DESC;
    END
END
GO

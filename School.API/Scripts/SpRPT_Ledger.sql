/* =====================================================================
   Account Ledger reports  (Student Ledger + Family Ledger)

   Two statements of account in the classic Dr/Cr form, built from the two
   tables that already record every fee movement:

       SMSStudentFee     one row per fee applied to a student  -> DEBIT
       FeeVoucherDetail  one row per fee settled by a voucher  -> CREDIT

   A voucher line is dated by its voucher header (when the money came in)
   but described by the fee it paid, so "Fee Voucher 0326-0440 / School Fee
   Apr-2026" reads as "this receipt cleared April's school fee" even when the
   receipt was taken in March. That is what makes an advance payment legible.

   Running balance is negative when the family has paid ahead. The reports
   render a negative balance as Cr and a positive one as Dr, which is why the
   procedures return the signed number and let the caller label it.

   Family grouping follows the STUDENT's ParentID, never the voucher's. The
   two disagree on a handful of rows where a child was moved between parent
   records, and following the student keeps a family's debits and credits on
   the same statement.

   Idempotent: safe to run repeatedly, and on a server older than 2016
   (hence the create-stub-then-alter dance rather than CREATE OR ALTER).

   Run with:
     sqlcmd -S . -U sa -P <pwd> -d EasySchoolDB -i SpRPT_Ledger.sql
   ===================================================================== */

USE [EasySchoolDB]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* ---------------------------------------------------------------------
   1. SpRPT_StudentLedger - statement of account for one student
   --------------------------------------------------------------------- */
IF OBJECT_ID('dbo.SpRPT_StudentLedger', 'P') IS NULL
    EXEC('CREATE PROCEDURE dbo.SpRPT_StudentLedger AS BEGIN SET NOCOUNT ON; END');
GO

ALTER PROCEDURE dbo.SpRPT_StudentLedger
(
    @StudentID INT,
    @DateFrom  DATE = NULL,
    @DateTo    DATE = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    -- An open-ended range means "everything", so the ledger can be pulled
    -- for the whole life of the account without inventing a start date.
    IF @DateFrom IS NULL SET @DateFrom = '1900-01-01';
    IF @DateTo   IS NULL SET @DateTo   = '9999-12-31';

    DECLARE @AccountTitle NVARCHAR(300),
            @AccountCode  NVARCHAR(100),
            @AccountInfo  NVARCHAR(600);

    SELECT  @AccountTitle = S.Name,
            -- Registration/Admission numbers are blank in many rows, so the
            -- roll number is the identifier that is actually printable.
            @AccountCode  = LTRIM(RTRIM(COALESCE(NULLIF(S.RegistrationNo, ''),
                                                 NULLIF(S.AdmissionNo, ''),
                                                 NULLIF(S.RollNo, ''),
                                                 CAST(S.VID AS NVARCHAR(20))))),
            -- "S/O Akmal Shahzad" for a boy, "D/O ..." for a girl.
            --
            -- Gender is dirty: most rows carry the legacy codes 1 and 0
            -- (1 = male, 0 = female — confirmed against the students' own
            -- names), some carry 'Male'/'Female', one is 'Malr', and 21 are
            -- blank. Anything unrecognised prints the father's name with NO
            -- prefix rather than guess: this statement goes to the parent, and
            -- calling a daughter S/O is worse than saying nothing.
            @AccountInfo  = LTRIM(RTRIM(
                                ISNULL(C.VName, '') +
                                CASE WHEN SEC.VName IS NULL THEN '' ELSE ' - ' + SEC.VName END +
                                CASE WHEN NULLIF(P.FatherName, '') IS NULL THEN ''
                                     ELSE '  |  ' +
                                          CASE WHEN LTRIM(RTRIM(ISNULL(S.Gender, ''))) = '1'
                                                 OR LTRIM(RTRIM(ISNULL(S.Gender, ''))) LIKE 'M%' THEN 'S/O '
                                               WHEN LTRIM(RTRIM(ISNULL(S.Gender, ''))) = '0'
                                                 OR LTRIM(RTRIM(ISNULL(S.Gender, ''))) LIKE 'F%' THEN 'D/O '
                                               ELSE '' END
                                          + P.FatherName END +
                                CASE WHEN NULLIF(P.FatherMobile, '') IS NULL THEN ''
                                     ELSE '  |  ' + P.FatherMobile END))
    FROM SMSStudent S
    LEFT JOIN SMSParent P ON P.VID = S.ParentID
    OUTER APPLY (
        SELECT TOP 1 SA.SectionID
        FROM SMSStudentAcademic SA
        WHERE SA.StudentID = S.VID AND ISNULL(SA.IsActive, 1) = 1
        ORDER BY SA.SessionID DESC, SA.VID DESC
    ) A
    LEFT JOIN SMSSection SEC ON SEC.VID = A.SectionID
    LEFT JOIN SMSClass   C   ON C.VID   = SEC.ClassID
    WHERE S.VID = @StudentID;

    ;WITH Lines AS
    (
        -- DEBIT: a fee applied to the student
        SELECT  F.VDate                                      AS LineDate,
                1                                            AS TypeOrder,
                CAST('Apply New Fee' AS NVARCHAR(50))        AS VoucherType,
                CAST(F.VID AS NVARCHAR(30))                  AS VNo,
                CAST(FT.VName + ' ' + FORMAT(F.VDate, 'MMM-yyyy') AS NVARCHAR(500)) AS [Description],
                CAST(F.FeeAmount AS DECIMAL(18, 2))          AS Debit,
                CAST(0 AS DECIMAL(18, 2))                    AS Credit,
                CAST(F.VID AS BIGINT)                        AS SortId,
                -- Time of day the row was entered, already formatted, and
                -- only when the entry landed on the line's own date. Fee
                -- postings are generated in bulk (a whole year dated
                -- 1st-of-month but inserted in one run), so most correctly show
                -- no time rather than a meaningless 12:00 AM.
                -- Formatted HERE, not in C#: InsertedDate is written with
                -- GETDATE() (server local, UTC+5), so serialising it as a
                -- DateTime would send it through the API's UTC instant policy
                -- and shift every time five hours. A string cannot be shifted.
                CASE WHEN CAST(F.InsertedDate AS DATE) = F.VDate
                     THEN FORMAT(F.InsertedDate, 'hh:mm tt', 'en-US') END AS EntryTime
        FROM SMSStudentFee F
        JOIN SMSFeeType FT ON FT.VID = F.FeeTypeID
        WHERE F.StudentID = @StudentID
          AND ISNULL(F.IsActive, 1) = 1

        UNION ALL

        -- CREDIT: a voucher line that settled one of those fees
        SELECT  H.VDate,
                2,
                CAST('Fee Voucher' AS NVARCHAR(50)),
                CAST(ISNULL(NULLIF(LTRIM(RTRIM(H.VNo)), ''), CAST(H.VID AS NVARCHAR(30))) AS NVARCHAR(30)),
                CAST(FT.VName + ' ' + FORMAT(F.VDate, 'MMM-yyyy') AS NVARCHAR(500)),
                CAST(0 AS DECIMAL(18, 2)),
                CAST(D.PaidAmount AS DECIMAL(18, 2)),
                CAST(D.VID AS BIGINT),
                CASE WHEN CAST(H.InsertedDate AS DATE) = H.VDate
                     THEN FORMAT(H.InsertedDate, 'hh:mm tt', 'en-US') END
        FROM FeeVoucherDetail D
        JOIN FeeVoucherMain H  ON H.VID  = D.MID
        JOIN SMSStudentFee  F  ON F.VID  = D.StudentFeeID
        JOIN SMSFeeType     FT ON FT.VID = F.FeeTypeID
        WHERE F.StudentID = @StudentID
          AND ISNULL(D.IsActive, 1) = 1
          AND ISNULL(H.IsActive, 1) = 1
    ),
    Running AS
    (
        -- The running total is taken over EVERY line, not just the ones in
        -- range, so a row's balance already carries the opening balance and
        -- the report never has to re-add it.
        SELECT  L.*,
                SUM(L.Debit - L.Credit) OVER (
                    ORDER BY L.LineDate, L.TypeOrder, L.SortId
                    ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS RunBalance
        FROM Lines L
    )
    SELECT * FROM
    (
        -- Opening balance: everything that happened before the window.
        SELECT  0                                    AS RowType,
                CAST(NULL AS DATE)                   AS LineDate,
                CAST(NULL AS NVARCHAR(10))           AS EntryTime,
                CAST('' AS NVARCHAR(50))             AS VoucherType,
                CAST('' AS NVARCHAR(30))             AS VNo,
                CAST('Opening Balance' AS NVARCHAR(500)) AS [Description],
                @StudentID                           AS StudentID,
                @AccountTitle                        AS StudentName,
                CAST(0 AS DECIMAL(18, 2))            AS Debit,
                CAST(0 AS DECIMAL(18, 2))            AS Credit,
                CAST(ISNULL((SELECT SUM(R.Debit - R.Credit) FROM Running R
                             WHERE R.LineDate < @DateFrom), 0) AS DECIMAL(18, 2)) AS Balance,
                CAST(NULL AS DATE)                   AS SortDate,
                0                                    AS TypeOrder,
                CAST(0 AS BIGINT)                    AS SortId

        UNION ALL

        SELECT  1,
                R.LineDate,
                R.EntryTime,
                R.VoucherType,
                R.VNo,
                R.[Description],
                @StudentID,
                @AccountTitle,
                R.Debit,
                R.Credit,
                CAST(R.RunBalance AS DECIMAL(18, 2)),
                R.LineDate,
                R.TypeOrder,
                R.SortId
        FROM Running R
        WHERE R.LineDate BETWEEN @DateFrom AND @DateTo
    ) X
    OUTER APPLY (SELECT @AccountTitle AS AccountTitle,
                        @AccountCode  AS AccountCode,
                        @AccountInfo  AS AccountInfo) H
    ORDER BY X.RowType, X.SortDate, X.TypeOrder, X.SortId;
END
GO


/* ---------------------------------------------------------------------
   2. SpRPT_FamilyLedger - one statement covering every child of a family

   Vouchers are already taken at family level (FeeVoucherMain.ParentID), so
   this is the statement that actually answers "what does this parent owe".
   Each line carries the child it belongs to so the report can group by
   student as well as show one continuous running balance.
   --------------------------------------------------------------------- */
IF OBJECT_ID('dbo.SpRPT_FamilyLedger', 'P') IS NULL
    EXEC('CREATE PROCEDURE dbo.SpRPT_FamilyLedger AS BEGIN SET NOCOUNT ON; END');
GO

ALTER PROCEDURE dbo.SpRPT_FamilyLedger
(
    @ParentID INT,
    @DateFrom DATE = NULL,
    @DateTo   DATE = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @DateFrom IS NULL SET @DateFrom = '1900-01-01';
    IF @DateTo   IS NULL SET @DateTo   = '9999-12-31';

    DECLARE @AccountTitle NVARCHAR(300),
            @AccountCode  NVARCHAR(100),
            @AccountInfo  NVARCHAR(600);

    SELECT  @AccountTitle = LTRIM(RTRIM(COALESCE(NULLIF(P.FatherName, ''),
                                                 NULLIF(P.GuardianName, ''),
                                                 'Family #' + CAST(P.VID AS NVARCHAR(20))))),
            @AccountCode  = LTRIM(RTRIM(COALESCE(NULLIF(P.FatherCNIC, ''),
                                                 NULLIF(P.GuardianCNIC, ''),
                                                 CAST(P.VID AS NVARCHAR(20))))),
            @AccountInfo  = ISNULL(NULLIF(P.FatherMobile, ''), ISNULL(P.GuardianMobile, ''))
    FROM SMSParent P
    WHERE P.VID = @ParentID;

    -- The children, appended to the account line so the statement names who
    -- it covers even when a child has no activity in the window.
    DECLARE @Children NVARCHAR(MAX) = STUFF((
        SELECT ', ' + S.Name
        FROM SMSStudent S
        WHERE S.ParentID = @ParentID AND ISNULL(S.IsActive, 1) = 1
        ORDER BY S.Name
        FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '');

    SET @AccountInfo = LTRIM(RTRIM(
            CASE WHEN @AccountInfo IS NULL THEN '' ELSE @AccountInfo + '  |  ' END +
            ISNULL(@Children, '')));

    ;WITH Lines AS
    (
        SELECT  F.VDate                                      AS LineDate,
                1                                            AS TypeOrder,
                CAST('Apply New Fee' AS NVARCHAR(50))        AS VoucherType,
                CAST(F.VID AS NVARCHAR(30))                  AS VNo,
                CAST(FT.VName + ' ' + FORMAT(F.VDate, 'MMM-yyyy') AS NVARCHAR(500)) AS [Description],
                S.VID                                        AS StudentID,
                S.Name                                       AS StudentName,
                CAST(F.FeeAmount AS DECIMAL(18, 2))          AS Debit,
                CAST(0 AS DECIMAL(18, 2))                    AS Credit,
                CAST(F.VID AS BIGINT)                        AS SortId,
                -- See the note in SpRPT_StudentLedger: pre-formatted, and only
                -- when the entry falls on the line's own date.
                CASE WHEN CAST(F.InsertedDate AS DATE) = F.VDate
                     THEN FORMAT(F.InsertedDate, 'hh:mm tt', 'en-US') END AS EntryTime
        FROM SMSStudentFee F
        JOIN SMSStudent S  ON S.VID  = F.StudentID
        JOIN SMSFeeType FT ON FT.VID = F.FeeTypeID
        WHERE S.ParentID = @ParentID
          AND ISNULL(F.IsActive, 1) = 1

        UNION ALL

        SELECT  H.VDate,
                2,
                CAST('Fee Voucher' AS NVARCHAR(50)),
                CAST(ISNULL(NULLIF(LTRIM(RTRIM(H.VNo)), ''), CAST(H.VID AS NVARCHAR(30))) AS NVARCHAR(30)),
                CAST(FT.VName + ' ' + FORMAT(F.VDate, 'MMM-yyyy') AS NVARCHAR(500)),
                S.VID,
                S.Name,
                CAST(0 AS DECIMAL(18, 2)),
                CAST(D.PaidAmount AS DECIMAL(18, 2)),
                CAST(D.VID AS BIGINT),
                CASE WHEN CAST(H.InsertedDate AS DATE) = H.VDate
                     THEN FORMAT(H.InsertedDate, 'hh:mm tt', 'en-US') END
        FROM FeeVoucherDetail D
        JOIN FeeVoucherMain H  ON H.VID  = D.MID
        JOIN SMSStudentFee  F  ON F.VID  = D.StudentFeeID
        JOIN SMSStudent     S  ON S.VID  = F.StudentID
        JOIN SMSFeeType     FT ON FT.VID = F.FeeTypeID
        WHERE S.ParentID = @ParentID
          AND ISNULL(D.IsActive, 1) = 1
          AND ISNULL(H.IsActive, 1) = 1
    ),
    Running AS
    (
        SELECT  L.*,
                SUM(L.Debit - L.Credit) OVER (
                    ORDER BY L.LineDate, L.TypeOrder, L.SortId
                    ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS RunBalance
        FROM Lines L
    )
    SELECT * FROM
    (
        SELECT  0                                    AS RowType,
                CAST(NULL AS DATE)                   AS LineDate,
                CAST(NULL AS NVARCHAR(10))           AS EntryTime,
                CAST('' AS NVARCHAR(50))             AS VoucherType,
                CAST('' AS NVARCHAR(30))             AS VNo,
                CAST('Opening Balance' AS NVARCHAR(500)) AS [Description],
                0                                    AS StudentID,
                CAST('' AS NVARCHAR(300))            AS StudentName,
                CAST(0 AS DECIMAL(18, 2))            AS Debit,
                CAST(0 AS DECIMAL(18, 2))            AS Credit,
                CAST(ISNULL((SELECT SUM(R.Debit - R.Credit) FROM Running R
                             WHERE R.LineDate < @DateFrom), 0) AS DECIMAL(18, 2)) AS Balance,
                CAST(NULL AS DATE)                   AS SortDate,
                0                                    AS TypeOrder,
                CAST(0 AS BIGINT)                    AS SortId

        UNION ALL

        SELECT  1,
                R.LineDate,
                R.EntryTime,
                R.VoucherType,
                R.VNo,
                R.[Description],
                R.StudentID,
                R.StudentName,
                R.Debit,
                R.Credit,
                CAST(R.RunBalance AS DECIMAL(18, 2)),
                R.LineDate,
                R.TypeOrder,
                R.SortId
        FROM Running R
        WHERE R.LineDate BETWEEN @DateFrom AND @DateTo
    ) X
    OUTER APPLY (SELECT @AccountTitle AS AccountTitle,
                        @AccountCode  AS AccountCode,
                        @AccountInfo  AS AccountInfo) H
    ORDER BY X.RowType, X.SortDate, X.TypeOrder, X.SortId;
END
GO


/* ---------------------------------------------------------------------
   3. SpGet_FamilyLookup - type-ahead for the Family Ledger picker

   Families are not a screen anywhere else in the app, so the report needs
   its own search: by father/guardian name, CNIC, mobile, or the name or
   roll number of any child.
   --------------------------------------------------------------------- */
IF OBJECT_ID('dbo.SpGet_FamilyLookup', 'P') IS NULL
    EXEC('CREATE PROCEDURE dbo.SpGet_FamilyLookup AS BEGIN SET NOCOUNT ON; END');
GO

ALTER PROCEDURE dbo.SpGet_FamilyLookup
(
    @Search NVARCHAR(200) = NULL,
    @Top    INT = 50
)
AS
BEGIN
    SET NOCOUNT ON;

    SET @Search = NULLIF(LTRIM(RTRIM(ISNULL(@Search, ''))), '');
    IF @Top IS NULL OR @Top <= 0 SET @Top = 50;

    SELECT TOP (@Top)
           P.VID AS ParentID,
           LTRIM(RTRIM(COALESCE(NULLIF(P.FatherName, ''),
                                NULLIF(P.GuardianName, ''),
                                'Family #' + CAST(P.VID AS NVARCHAR(20))))) AS FatherName,
           ISNULL(NULLIF(P.FatherCNIC, ''), ISNULL(P.GuardianCNIC, ''))     AS FatherCNIC,
           ISNULL(NULLIF(P.FatherMobile, ''), ISNULL(P.GuardianMobile, '')) AS FatherMobile,
           K.StudentCount,
           K.StudentNames
    FROM SMSParent P
    CROSS APPLY (
        SELECT COUNT(*) AS StudentCount,
               STUFF((SELECT ', ' + S2.Name
                      FROM SMSStudent S2
                      WHERE S2.ParentID = P.VID AND ISNULL(S2.IsActive, 1) = 1
                      ORDER BY S2.Name
                      FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS StudentNames
        FROM SMSStudent S1
        WHERE S1.ParentID = P.VID AND ISNULL(S1.IsActive, 1) = 1
    ) K
    WHERE K.StudentCount > 0
      AND (
            @Search IS NULL
            OR P.FatherName    LIKE '%' + @Search + '%'
            OR P.GuardianName  LIKE '%' + @Search + '%'
            OR P.FatherCNIC    LIKE '%' + @Search + '%'
            OR P.FatherMobile  LIKE '%' + @Search + '%'
            OR K.StudentNames  LIKE '%' + @Search + '%'
            OR EXISTS (SELECT 1 FROM SMSStudent S3
                       WHERE S3.ParentID = P.VID
                         AND ISNULL(S3.IsActive, 1) = 1
                         AND S3.RollNo LIKE '%' + @Search + '%')
          )
    ORDER BY FatherName;
END
GO

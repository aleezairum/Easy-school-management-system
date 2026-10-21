/* ============================================================================
   SpGetPending_SMSStudentFee — what a family owes, for the Fee Voucher screen
   ============================================================================

   Deliberately NO "USE [EasySchoolDB]" here: a hard-coded USE overrides
   sqlcmd's -d and sends the script to whatever that name resolves to on the
   machine it runs on. The database comes from -d instead.

     cd School.API\Scripts
     sqlcmd -S . -U sa -P <pwd> -d EasySchoolDB -i SpGetPending_SMSStudentFee.sql

   THE BUG THIS FIXES
   ------------------
   Fees are applied months in advance — a student can have rows raised for
   every month to December. This procedure returned every unpaid row with no
   regard for its date, so when a parent paid September the receipt announced
   October, November and December as "pending" too, and the counter had to
   explain that the family did not, in fact, owe it yet.

   Everywhere else in the application already draws the line at today:

     SpGetPending_SMSStudentFee_Msg   AND CAST(F.VDate AS DATE) <= @Today
     WaBotRepository.GetPendingDuesAsync   the same
     SpRPT_SMSStudentFee (Unpaid)     AND F.VDate < GETDATE()

   so this was the one place out of step.

   WHAT IT DOES NOW
   ----------------
   The future rows are still RETURNED, because a parent who wants to pay three
   months up front must be able to: dropping them would trade one wrong answer
   for another. Each row now says whether it is due, and the screen adds up the
   two separately — "payable now" against "paid in advance".

     @AsOn   the day to judge by. Defaults to today; the voucher screen passes
             the voucher's own date, so a back-dated receipt is worked out as
             at the day it is dated rather than the day it is printed.
   ============================================================================ */
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('dbo.SpGetPending_SMSStudentFee', 'P') IS NULL
    EXEC('CREATE PROCEDURE dbo.SpGetPending_SMSStudentFee AS SET NOCOUNT ON;');
GO

ALTER PROCEDURE [dbo].[SpGetPending_SMSStudentFee]
(
    @StudentID INT,
    @AsOn      DATE = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Cut DATE = ISNULL(@AsOn, CAST(GETDATE() AS DATE));

    -- The whole family's dues, not one child's: a parent pays for every child
    -- at one counter, which is why this has always keyed on the parent.
    DECLARE @ParentID INT;
    SELECT @ParentID = ParentID FROM dbo.SMSStudent WHERE VID = @StudentID;

    SELECT
        F.VID,
        F.VDate,
        S.Name       AS StudentName,
        C.VName      AS ClassName,
        SY.VName     AS SessionName,
        SEC.VName    AS SectionName,
        G.VName      AS GradeName,
        FT.VName     AS FeeTypeName,
        F.FeeAmount,
        F.ClearedAmount,
        F.FeeAmount - F.ClearedAmount AS PendingAmount,
        F.Remarks,
        F.IsClear,

        -- 1 = owed today. 0 = raised for a month still to come, so it counts
        -- as an advance payment rather than as arrears.
        CAST(CASE WHEN CAST(F.VDate AS DATE) <= @Cut THEN 1 ELSE 0 END AS BIT) AS IsDue

    FROM dbo.SMSStudentFee F
    LEFT JOIN dbo.SMSStudent          S   ON F.StudentID = S.VID
    LEFT JOIN dbo.AcademicSessionYear SY  ON F.SessionID = SY.VID
    LEFT JOIN dbo.SMSSection          SEC ON F.SectionID = SEC.VID
    LEFT JOIN dbo.SMSClass            C   ON SEC.ClassID = C.VID
    LEFT JOIN dbo.AcademicGrades      G   ON F.GradeID   = G.VID
    LEFT JOIN dbo.SMSFeeType          FT  ON F.FeeTypeID = FT.VID
    WHERE S.ParentID = @ParentID
      AND F.IsActive = 1
      AND F.FeeAmount - F.ClearedAmount > 0
      AND F.IsClear = 0
    -- Due months first, oldest first: the counter works down the arrears and
    -- the advance months sit below, out of the way.
    ORDER BY CASE WHEN CAST(F.VDate AS DATE) <= @Cut THEN 0 ELSE 1 END,
             F.VDate, F.SessionID, F.SectionID;
END
GO

SELECT 'SpGetPending_SMSStudentFee: ' +
       CASE WHEN OBJECT_DEFINITION(OBJECT_ID('dbo.SpGetPending_SMSStudentFee')) LIKE '%IsDue%'
            THEN 'updated — rows now say whether they are due'
            ELSE 'NOT updated' END;
GO

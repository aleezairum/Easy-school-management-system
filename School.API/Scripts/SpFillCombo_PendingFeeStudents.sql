/* ============================================================================
   SpFillCombo_PendingFeeStudents — the students a counter clerk actually needs
   ============================================================================

   No "USE [EasySchoolDB]" on purpose: a hard-coded USE overrides sqlcmd's -d.
   The database comes from -d instead.

     cd School.API\Scripts
     sqlcmd -S . -U sa -P <pwd> -d EasySchoolDB -i SpFillCombo_PendingFeeStudents.sql

   WHY
   ---
   The Receive Payment screen filled its Class / Section / Roll / Name / Father
   dropdowns from SpFillCombo_SMSStudent, which lists every enrolled child —
   1,226 of them. Only about 305 owe anything, so the clerk was scrolling past
   four names that owe nothing for every one that does.

   This returns the same columns as SpFillCombo_SMSStudent so the screen fills
   the same dropdowns, and adds what the clerk wants to see: how much is owed
   and over how many months.

   THE CUT-OFF
   -----------
   Fees are raised months ahead, so "pending" has to mean pending AS AT a date,
   not "unpaid at any point in the future". The rule is the one the rest of the
   application already uses:

     SpGetPending_SMSStudentFee       CAST(F.VDate AS DATE) <= @AsOn
     SpGetPending_SMSStudentFee_Msg   the same
     WaBotRepository.GetPendingDuesAsync   the same
     SpRPT_SMSStudentFee (Unpaid)     F.VDate < GETDATE()

     @AsOn  the day to judge by; the screen passes the voucher's own date so a
            back-dated voucher lists whoever owed on that day. NULL means today.

   Dues are counted per student, not per family. The voucher itself still
   settles the whole family's fees once a child is picked — that is deliberate
   and unchanged — but a child who owes nothing has no reason to be in the list.
   ============================================================================ */
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('dbo.SpFillCombo_PendingFeeStudents', 'P') IS NULL
    EXEC('CREATE PROCEDURE dbo.SpFillCombo_PendingFeeStudents AS SET NOCOUNT ON;');
GO

ALTER PROCEDURE [dbo].[SpFillCombo_PendingFeeStudents]
(
    @ClassID   INT  = NULL,
    @SectionID INT  = NULL,
    @AsOn      DATE = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Cut DATE = ISNULL(@AsOn, CAST(GETDATE() AS DATE));

    SELECT
        S.VID,
        S.RollNo                                        AS RollNo,
        S.Name + ' ' + Sec.VName + ' ' + C.VName        AS Name,
        S.Name_Urdu,
        A.RollNo                                        AS ClassRollNo,
        S.ParentID,
        P.FatherName,
        P.FatherCNIC,
        D.PendingAmount,
        D.MonthsDue

    FROM SMSStudent S
    LEFT  OUTER JOIN SMSParent          P   ON S.ParentID  = P.VID
    INNER JOIN       SMSStudentAcademic A   ON S.VID       = A.StudentID
    INNER JOIN       SMSSection         Sec ON A.SectionID = Sec.VID
    INNER JOIN       SMSClass           C   ON Sec.ClassID = C.VID

    -- The join is what does the filtering: no unpaid row dated on or before
    -- the cut-off, no place in the list.
    INNER JOIN (
        SELECT F.StudentID,
               SUM(F.FeeAmount - F.ClearedAmount) AS PendingAmount,
               COUNT(*)                           AS MonthsDue
        FROM SMSStudentFee F
        WHERE F.IsActive = 1
          AND F.IsClear  = 0
          AND F.FeeAmount - F.ClearedAmount > 0
          AND CAST(F.VDate AS DATE) <= @Cut
        GROUP BY F.StudentID
    ) D ON D.StudentID = S.VID

    WHERE (@ClassID   IS NULL OR @ClassID   = 0 OR Sec.ClassID  = @ClassID)
      AND (@SectionID IS NULL OR @SectionID = 0 OR A.SectionID  = @SectionID)
      AND A.StatusID = 1

    ORDER BY A.SectionID, CAST(A.RollNo AS INT);
END
GO

SELECT 'SpFillCombo_PendingFeeStudents: ' +
       CASE WHEN OBJECT_DEFINITION(OBJECT_ID('dbo.SpFillCombo_PendingFeeStudents')) LIKE '%MonthsDue%'
            THEN 'ready'
            ELSE 'NOT created' END;
GO

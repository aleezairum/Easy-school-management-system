USE [EasySchoolDB]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
/* ============================================================================
   SMS module update
     1. SmsMessages.ForcePaid  — a row that must skip WAHA (free) and go out
        through the paid gateway. Set when the user picks "Send via Paid"
        after WAHA was found disconnected.
        Status = 3 (Held) is a new SmsStatus value; no schema change needed
        because Status is already an INT.
     2. SpGetPending_SMSStudentFee_Msg — new @TillDate filter. Students are
        selected by dues pending up to @TillDate, but every one of their
        pending dues up to TODAY is returned.
   ========================================================================= */

-- ─────────────────────────────────────────────────────────────────────────
-- 1. ForcePaid column
-- ─────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'SmsMessages' AND COLUMN_NAME = 'ForcePaid')
BEGIN
    ALTER TABLE dbo.SmsMessages
        ADD ForcePaid BIT NOT NULL CONSTRAINT DF_SmsMessages_ForcePaid DEFAULT (0);
END
GO

-- Held rows are polled by the UI and resumed in bulk, so make that lookup cheap.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_SmsMessages_Status_JobId' AND object_id = OBJECT_ID('dbo.SmsMessages'))
BEGIN
    CREATE INDEX IX_SmsMessages_Status_JobId
        ON dbo.SmsMessages (Status, JobId) INCLUDE (MessageType);
END
GO

-- ─────────────────────────────────────────────────────────────────────────
-- 2. SpGetPending_SMSStudentFee_Msg  (+ @TillDate)
-- ─────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SpGetPending_SMSStudentFee_Msg', 'P') IS NULL
    EXEC('CREATE PROCEDURE dbo.SpGetPending_SMSStudentFee_Msg AS SET NOCOUNT ON;');
GO

ALTER PROCEDURE [dbo].[SpGetPending_SMSStudentFee_Msg]
(
    @VID       INT = NULL,
    @StudentID INT = NULL,
    @SessionID INT = NULL,
    @ClassID   INT = NULL,
    @SectionID INT = NULL,
    @TillDate  DATE = NULL      -- NEW: pick students by dues pending up to this date
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Today DATE = CAST(GETDATE() AS DATE);
    DECLARE @Till  DATE = ISNULL(@TillDate, @Today);

    -- A till-date in the future would widen the student list beyond what is
    -- actually due, so it is clamped to today.
    IF @Till > @Today SET @Till = @Today;

    -- ── Step 1 ──────────────────────────────────────────────────────────
    -- Who gets a reminder: students carrying at least one unpaid fee row
    -- dated on or before @Till. A March/April defaulter qualifies here.
    DECLARE @Students TABLE (StudentID INT PRIMARY KEY);

    INSERT INTO @Students (StudentID)
    SELECT DISTINCT F.StudentID
    FROM SMSStudentFee F
    LEFT JOIN SMSStudent         S   ON F.StudentID  = S.VID
    LEFT JOIN SMSStudentAcademic SA  ON SA.StudentID = S.VID
    LEFT JOIN SMSSection         SEC ON SA.SectionID = SEC.VID
    WHERE F.FeeAmount - F.ClearedAmount > 0
      AND CAST(F.VDate AS DATE) <= @Till
      AND F.IsActive = 1
      AND SA.StatusID = 1
      -- A parent who has promised to pay by a date that has not yet passed
      -- gets no reminder until it does. Same rule as the unpaid report, asked
      -- of the same view, so a child can never be off one list and on the other.
      AND NOT EXISTS (SELECT 1 FROM dbo.vwActiveFeePromise PRM WHERE PRM.StudentID = F.StudentID)
      AND (@VID       IS NULL OR @VID       = 0 OR F.VID       = @VID)
      AND (@StudentID IS NULL OR @StudentID = 0 OR F.StudentID = @StudentID)
      AND (@SessionID IS NULL OR @SessionID = 0 OR F.SessionID = @SessionID)
      AND (@SectionID IS NULL OR @SectionID = 0 OR F.SectionID = @SectionID)
      AND (@ClassID   IS NULL OR @ClassID   = 0 OR SEC.ClassID = @ClassID);

    -- ── Step 2 ──────────────────────────────────────────────────────────
    -- What the reminder lists: EVERY unpaid fee row those students still
    -- carry, up to today. So the March/April defaulter above is also shown
    -- May, June, July and August if those are pending too.
    SELECT
        F.VID,
        F.StudentID,
        F.VDate,
        S.Name          AS StudentName,
        P.FatherMobile,
        P.GuardianMobile,
        P.MotherMobile,
        S.MobileNo,
        P.IsFatherSMS,
        P.IsGuardianSMS,
        P.IsMotherSMS,
        S.IsSMS,
        C.VName         AS ClassName,
        SY.VName        AS SessionName,
        SEC.VName       AS SectionName,
        G.VName         AS GradeName,
        FT.VName        AS FeeTypeName,
        F.FeeAmount,
        F.ClearedAmount,
        F.FeeAmount - F.ClearedAmount AS PendingAmount,
        F.Remarks,
        F.IsClear
    FROM SMSStudentFee F
    INNER JOIN @Students          ST  ON ST.StudentID = F.StudentID
    LEFT  JOIN SMSStudent         S   ON F.StudentID  = S.VID
    LEFT  JOIN SMSStudentAcademic SA  ON SA.StudentID = S.VID
    LEFT  JOIN SMSParent          P   ON S.ParentID   = P.VID
    LEFT  JOIN AcademicSessionYear SY ON F.SessionID  = SY.VID
    LEFT  JOIN SMSSection         SEC ON SA.SectionID = SEC.VID
    LEFT  JOIN SMSClass           C   ON SEC.ClassID  = C.VID
    LEFT  JOIN AcademicGrades     G   ON SA.GradeID   = G.VID
    LEFT  JOIN SMSFeeType         FT  ON F.FeeTypeID  = FT.VID
    WHERE F.FeeAmount - F.ClearedAmount > 0
      AND CAST(F.VDate AS DATE) <= @Today
      AND F.IsActive = 1
      AND SA.StatusID = 1
      -- A parent who has promised to pay by a date that has not yet passed
      -- gets no reminder until it does. Same rule as the unpaid report, asked
      -- of the same view, so a child can never be off one list and on the other.
      AND NOT EXISTS (SELECT 1 FROM dbo.vwActiveFeePromise PRM WHERE PRM.StudentID = F.StudentID)
      -- @VID/@TillDate only chose the students in step 1; they must not
      -- narrow the dues listed here.
      AND (@SessionID IS NULL OR @SessionID = 0 OR F.SessionID = @SessionID)
    ORDER BY F.StudentID, F.VDate;
END
GO

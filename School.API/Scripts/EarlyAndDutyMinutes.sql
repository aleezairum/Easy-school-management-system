/* ============================================================================
   Early leaving, and the duty swap, charged per minute
   ============================================================================

   Deliberately NO "USE [EasySchoolDB]" here: a hard-coded USE overrides
   sqlcmd's -d and sends the script to whatever that name resolves to on the
   machine it runs on. The database comes from -d instead.

     cd School.API\Scripts
     sqlcmd -S . -U sa -P <pwd> -d EasySchoolDB -i EarlyAndDutyMinutes.sql

   WHAT THIS ADDS

   1. EARLY LEAVING IS NOW CHARGED.
      Attendance has always recorded EarlyMinutes and payroll never read them:
      a teacher who left half an hour early lost nothing, while one who arrived
      half an hour late paid for it. Same rate, same rule, no grace:

        one minute = Gross / (working days in the month x hours in the day x 60)

      Working days means the month's days less public holidays and less that
      employee's own weekly off days.

   2. THE DUTY SWAP - two new columns on HrAttendance.
      When one member of staff covers another's duty, the office enters the
      minutes on both sides:

        ShortMinutes   minutes of this person's duty somebody else covered
        ExtraMinutes   minutes this person worked covering for somebody else

      Payroll charges (short - extra) at the same per-minute rate. The figure
      goes NEGATIVE for somebody who covered more than was covered for them,
      and a negative deduction adds to the pay - the bonus the school asked
      for. The account settles between the two people at one rate.

      It is a DEDUCTION component and not an earning on purpose: earnings are
      added to gross before deductions are worked out, and every per-minute
      rate here is derived FROM gross, so an extra-duty earning would raise the
      very rate used to price that person's own lateness.

   NOTE ON THE DAY'S STATUS
   The application no longer downgrades a late arrival or an early leaver to a
   half day. Marked in means Present; not marked means Absent; the minutes are
   what carry the penalty. That is a code change (AttendanceCalculator), not a
   database one, and it only affects attendance saved from now on. Days already
   graded Half Day keep that grade until they are saved again.

   RE-RUN THE PAYROLL AFTERWARDS. Payslips already generated keep the figures
   they were generated with.

   QUOTED_IDENTIFIER: Employees and HrAttendance are written to below, and
   Employees carries a computed column, so the option must be ON. It is set at
   the top of this script.
   ============================================================================ */
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* ── 1. The two duty-swap columns ───────────────────────────────────────── */

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.HrAttendance') AND name = 'ShortMinutes')
BEGIN
    ALTER TABLE dbo.HrAttendance
        ADD ShortMinutes INT NOT NULL CONSTRAINT DF_HrAttendance_ShortMinutes DEFAULT (0);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.HrAttendance') AND name = 'ExtraMinutes')
BEGIN
    ALTER TABLE dbo.HrAttendance
        ADD ExtraMinutes INT NOT NULL CONSTRAINT DF_HrAttendance_ExtraMinutes DEFAULT (0);
END
GO

/* ── 2. Early leaving ───────────────────────────────────────────────────── */

DECLARE @EffectiveFrom DATE = '2026-09-01';   -- same month the late rule starts

IF NOT EXISTS (SELECT 1 FROM dbo.HrPayComponent WHERE Code = 'EARLY')
BEGIN
    INSERT dbo.HrPayComponent
        (Code, Name, Kind, CalcType, Rate, RateSource, BaseOn,
         DivisorDays, DivisorHours, GraceMinutes, FreeUnitsPerMonth,
         CapPerDay, CapPerMonth, SlabsJson, Formula,
         ScopeType, ScopeId, Priority, ShowOnPayslip, EffectiveFrom, EffectiveTo, IsActive)
    VALUES
        ('EARLY', 'Early Leaving',
         2,      -- Deduction
         9,      -- PerEarlyMinute
         NULL,   -- worked out from the salary
         2,      -- Derived
         2,      -- of Gross
         255,    -- WorkingDays: the month less holidays and weekly offs
         NULL,   -- each employee's own daily hours
         0,      -- no grace, as with lateness
         NULL, NULL, NULL, NULL, NULL,
         1, NULL, 0, 1, @EffectiveFrom, NULL, 1);
END
ELSE
BEGIN
    UPDATE dbo.HrPayComponent
       SET Name = 'Early Leaving', Kind = 2, CalcType = 9,
           Rate = NULL, RateSource = 2, BaseOn = 2,
           DivisorDays = 255, DivisorHours = NULL, GraceMinutes = 0,
           FreeUnitsPerMonth = NULL, CapPerDay = NULL, CapPerMonth = NULL,
           ScopeType = 1, ScopeId = NULL, ShowOnPayslip = 1,
           EffectiveFrom = @EffectiveFrom, EffectiveTo = NULL, IsActive = 1
     WHERE Code = 'EARLY';
END

/* ── 3. The duty swap ───────────────────────────────────────────────────── */

IF NOT EXISTS (SELECT 1 FROM dbo.HrPayComponent WHERE Code = 'DUTYSWAP')
BEGIN
    INSERT dbo.HrPayComponent
        (Code, Name, Kind, CalcType, Rate, RateSource, BaseOn,
         DivisorDays, DivisorHours, GraceMinutes, FreeUnitsPerMonth,
         CapPerDay, CapPerMonth, SlabsJson, Formula,
         ScopeType, ScopeId, Priority, ShowOnPayslip, EffectiveFrom, EffectiveTo, IsActive)
    VALUES
        ('DUTYSWAP', 'Duty Cover (short less extra)',
         2,      -- Deduction, and negative when it is a bonus
         10,     -- PerDutyMinute
         NULL, 2, 2, 255, NULL, 0,
         NULL,
         NULL,   -- no per-day cap: this is a monthly account, not a daily one
         NULL,   -- and no monthly cap, which would clip the bonus side
         NULL, NULL,
         1, NULL, 0, 1, @EffectiveFrom, NULL, 1);
END
ELSE
BEGIN
    UPDATE dbo.HrPayComponent
       SET Name = 'Duty Cover (short less extra)', Kind = 2, CalcType = 10,
           Rate = NULL, RateSource = 2, BaseOn = 2,
           DivisorDays = 255, DivisorHours = NULL, GraceMinutes = 0,
           FreeUnitsPerMonth = NULL, CapPerDay = NULL, CapPerMonth = NULL,
           ScopeType = 1, ScopeId = NULL, ShowOnPayslip = 1,
           EffectiveFrom = @EffectiveFrom, EffectiveTo = NULL, IsActive = 1
     WHERE Code = 'DUTYSWAP';
END
GO

/* ── What is now in force ───────────────────────────────────────────────── */
SELECT 'HrAttendance.ShortMinutes: ' + CASE WHEN EXISTS (SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.HrAttendance') AND name = 'ShortMinutes') THEN 'ready' ELSE 'missing' END;
SELECT 'HrAttendance.ExtraMinutes: ' + CASE WHEN EXISTS (SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.HrAttendance') AND name = 'ExtraMinutes') THEN 'ready' ELSE 'missing' END;

SELECT Code,
       Name,
       CASE CalcType WHEN 3  THEN 'Per late minute'
                     WHEN 8  THEN 'Per minute out of school'
                     WHEN 9  THEN 'Per early minute'
                     WHEN 10 THEN 'Per duty minute (short less extra)'
                     ELSE CAST(CalcType AS VARCHAR) END AS Charged,
       CASE DivisorDays WHEN 255 THEN 'Working days (less holidays)'
                        ELSE CAST(DivisorDays AS VARCHAR) END AS DividedBy,
       GraceMinutes,
       IsActive
FROM   dbo.HrPayComponent
WHERE  Code IN ('LATE', 'EARLY', 'SHORTLEAVE', 'DUTYSWAP')
ORDER BY Code;
GO

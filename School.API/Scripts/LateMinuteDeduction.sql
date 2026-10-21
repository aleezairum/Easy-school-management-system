/* ============================================================================
   Late arrival, and short leave, priced per minute
   ============================================================================

   Deliberately NO "USE [EasySchoolDB]" here: a hard-coded USE overrides
   sqlcmd's -d and sends the script to whatever that name resolves to on the
   machine it runs on. The database comes from -d instead.

     cd School.API\Scripts
     sqlcmd -S . -U sa -P <pwd> -d EasySchoolDB -i LateMinuteDeduction.sql

   THE PROBLEM THIS FIXES
   ----------------------
   The Payroll Review screen showed late minutes against every teacher and
   deducted nothing - Deduction read 0 down the whole sheet. Nothing was
   broken: payroll only deducts what the Pay Components say to deduct, and
   there was no late component. The one component on file, SHORTLEAVE, was
   switched off with a rate of zero, so the short-leave deduction the school
   asked for was not running either.

   THE RULE THE SCHOOL CHOSE
   -------------------------
     one minute = Gross / (working days in the month x hours in the day x 60)

   Working days means the month's days less public holidays and less that
   employee's own weekly off days, which is exactly what HrDivisorDays 255
   (WorkingDays) resolves to. Hours in the day is left blank so each employee
   is measured against their own timing - a Principal and a guard do not keep
   the same hours - falling back to 8 where no timing has been entered.

   No grace: the first minute counts.

   So a teacher on 21,500 gross, in a 26-working-day month of 8-hour days:

       21,500 / (26 x 8 x 60) = 21,500 / 12,480 = Rs 1.72 per minute
       40 minutes late over the month                = Rs 69

   Nothing is hard-coded per employee. The figure follows each salary, each
   timing and each month's holiday list on its own.

   Short leave is priced by the same rule, on the same basis, which is what
   the school asked for: a minute out of school during the day costs what a
   minute late in the morning costs.

   RE-RUN THE PAYROLL AFTERWARDS. Payslips already generated keep the figures
   they were generated with; this changes what the NEXT calculation does.
   ============================================================================ */
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

DECLARE @EffectiveFrom DATE = '2026-09-01';   -- covers the September run on screen

/* ── 1. Late arrival ────────────────────────────────────────────────────── */

IF NOT EXISTS (SELECT 1 FROM dbo.HrPayComponent WHERE Code = 'LATE')
BEGIN
    INSERT dbo.HrPayComponent
        (Code, Name, Kind, CalcType, Rate, RateSource, BaseOn,
         DivisorDays, DivisorHours, GraceMinutes, FreeUnitsPerMonth,
         CapPerDay, CapPerMonth, SlabsJson, Formula,
         ScopeType, ScopeId, Priority, ShowOnPayslip, EffectiveFrom, EffectiveTo, IsActive)
    VALUES
        ('LATE', 'Late Arrival',
         2,      -- Kind: Deduction
         3,      -- CalcType: PerLateMinute
         NULL,   -- Rate: none, it is worked out from the salary
         2,      -- RateSource: Derived
         2,      -- BaseOn: Gross
         255,    -- DivisorDays: WorkingDays (month less holidays and weekly offs)
         NULL,   -- DivisorHours: each employee's own timing
         0,      -- GraceMinutes: none, the first minute counts
         NULL, NULL, NULL, NULL, NULL,
         1,      -- ScopeType: Global, every employee
         NULL, 0, 1, @EffectiveFrom, NULL, 1);
END
ELSE
BEGIN
    UPDATE dbo.HrPayComponent
       SET Name = 'Late Arrival', Kind = 2, CalcType = 3,
           Rate = NULL, RateSource = 2, BaseOn = 2,
           DivisorDays = 255, DivisorHours = NULL, GraceMinutes = 0,
           FreeUnitsPerMonth = NULL, CapPerDay = NULL, CapPerMonth = NULL,
           ScopeType = 1, ScopeId = NULL, ShowOnPayslip = 1,
           EffectiveFrom = @EffectiveFrom, EffectiveTo = NULL, IsActive = 1
     WHERE Code = 'LATE';
END

/* ── 2. Short leave, priced the same way ────────────────────────────────── */

IF NOT EXISTS (SELECT 1 FROM dbo.HrPayComponent WHERE Code = 'SHORTLEAVE')
BEGIN
    INSERT dbo.HrPayComponent
        (Code, Name, Kind, CalcType, Rate, RateSource, BaseOn,
         DivisorDays, DivisorHours, GraceMinutes, FreeUnitsPerMonth,
         CapPerDay, CapPerMonth, SlabsJson, Formula,
         ScopeType, ScopeId, Priority, ShowOnPayslip, EffectiveFrom, EffectiveTo, IsActive)
    VALUES
        ('SHORTLEAVE', 'Short Leave (time out of school)',
         2, 8,   -- Deduction, PerOutingMinute
         NULL, 2, 2, 255, NULL, 0,
         NULL, NULL, NULL, NULL, NULL,
         1, NULL, 0, 1, @EffectiveFrom, NULL, 1);
END
ELSE
BEGIN
    -- It was sitting switched off with a rate of zero, so it deducted nothing.
    UPDATE dbo.HrPayComponent
       SET Kind = 2, CalcType = 8,
           Rate = NULL, RateSource = 2, BaseOn = 2,
           DivisorDays = 255, DivisorHours = NULL, GraceMinutes = 0,
           FreeUnitsPerMonth = NULL, CapPerDay = NULL, CapPerMonth = NULL,
           ScopeType = 1, ScopeId = NULL, ShowOnPayslip = 1,
           EffectiveFrom = @EffectiveFrom, EffectiveTo = NULL, IsActive = 1
     WHERE Code = 'SHORTLEAVE';
END
GO

/* ── What is now in force ───────────────────────────────────────────────── */
SELECT Code,
       Name,
       CASE Kind WHEN 1 THEN 'Earning' ELSE 'Deduction' END      AS Kind,
       CASE CalcType WHEN 3 THEN 'Per late minute'
                     WHEN 8 THEN 'Per minute out of school'
                     ELSE CAST(CalcType AS VARCHAR) END           AS Charged,
       CASE RateSource WHEN 2 THEN 'From salary' ELSE 'Fixed' END AS RateFrom,
       CASE BaseOn WHEN 2 THEN 'Gross' ELSE 'Basic' END           AS BasedOn,
       CASE DivisorDays WHEN 255 THEN 'Working days (less holidays)'
                        WHEN 0   THEN 'Calendar days'
                        ELSE CAST(DivisorDays AS VARCHAR) + ' days' END AS DividedBy,
       ISNULL(CAST(DivisorHours AS VARCHAR), 'each employee''s own') AS HoursPerDay,
       GraceMinutes,
       IsActive
FROM   dbo.HrPayComponent
WHERE  Code IN ('LATE', 'SHORTLEAVE')
ORDER BY Code;
GO

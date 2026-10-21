/* ============================================================================
   Leave deduction (and the absent deduction it is priced like)
   ----------------------------------------------------------------------------
   Run on the target database:
     sqlcmd -S . -U sa -P <pwd> -d EasySchoolDB -i LeaveDeduction.sql

   LEAVE    "Leave Deduction" - CalcType 11, Per Leave Day. Every day marked
            Leave (attendance register, or an approved leave application) is
            charged one day's pay: Basic / days in the month, the sheet's Daily
            Pay. A day with a Leave Type costs that type's Deduct Days (Payroll
            Setup -> Leave Types: 0 paid, 1 unpaid, 0.5 half); a day with no
            type costs a full day. Free Units/Month on the rule = leave days a
            month that stay paid (0 here). Untick Active and all leave is paid.

   ABSENT   "Absent Deduction" - CalcType 4, Per Absent Day, priced the same.
            Added only when no Per Absent Day rule exists yet.

   Both effective from 1 September 2026, so September's payroll takes them
   when it is generated again. Safe to run twice: each is added only when no
   rule of its calc type exists, and nothing existing is changed.
   ============================================================================ */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

DECLARE @EffectiveFrom DATE = '2026-09-01';

IF NOT EXISTS (SELECT 1 FROM dbo.HrPayComponent WHERE CalcType = 11)
    INSERT dbo.HrPayComponent
        (Code, Name, Kind, CalcType, Rate, RateSource, BaseOn,
         DivisorDays, DivisorHours, GraceMinutes, FreeUnitsPerMonth,
         CapPerDay, CapPerMonth, SlabsJson, Formula,
         ScopeType, ScopeId, Priority, ShowOnPayslip, EffectiveFrom, EffectiveTo, IsActive)
    VALUES
        ('LEAVE', 'Leave Deduction',
         2,      -- Kind: Deduction
         11,     -- CalcType: PerLeaveDay
         NULL,   -- Rate: worked out from the salary
         2,      -- RateSource: Derived
         1,      -- BaseOn: Basic
         0,      -- DivisorDays: Calendar (days in the month) - the sheet's Daily Pay
         NULL, 0,
         0,      -- FreeUnitsPerMonth: no leave day paid by the rule itself
         NULL, NULL, '[]', NULL,
         1, NULL, 0, 1, @EffectiveFrom, NULL, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.HrPayComponent WHERE CalcType = 4)
    INSERT dbo.HrPayComponent
        (Code, Name, Kind, CalcType, Rate, RateSource, BaseOn,
         DivisorDays, DivisorHours, GraceMinutes, FreeUnitsPerMonth,
         CapPerDay, CapPerMonth, SlabsJson, Formula,
         ScopeType, ScopeId, Priority, ShowOnPayslip, EffectiveFrom, EffectiveTo, IsActive)
    VALUES
        ('ABSENT', 'Absent Deduction',
         2, 4,   -- Deduction, PerAbsentDay
         NULL, 2, 1, 0, NULL, 0, 0,
         NULL, NULL, '[]', NULL,
         1, NULL, 0, 1, @EffectiveFrom, NULL, 1);
GO

SELECT 'Rule ' + Code + ' : CalcType ' + CAST(CalcType AS VARCHAR(5)) + CASE WHEN IsActive = 1 THEN '' ELSE ' (off)' END
  FROM dbo.HrPayComponent WHERE CalcType IN (4, 11) ORDER BY CalcType;

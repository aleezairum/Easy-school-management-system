/* ============================================================================
   Per-minute rate on the payslip, and the minute rules put right
   ============================================================================

   Deliberately NO "USE [EasySchoolDB]" here: a hard-coded USE overrides
   sqlcmd's -d. The database comes from -d instead.

     cd School.API\Scripts
     sqlcmd -S . -U sa -P <pwd> -d EasySchoolDB -i PayrollMinuteRate.sql

   1. THE RULES
      Payroll Setup -> Pay Components could not show the calc types of the
      out-of-school (8), early-leaving (9) and duty-cover (10) rules, and
      saving one there turned it into "Per Late Minute" (3). Each of them then
      charged the LATE minutes again, and early leaving and time out of school
      were not charged at all. The screen is fixed; this puts the rules back,
      by their codes, only where they were turned into 3.

   2. THE RATE ON THE PAYSLIP
      HrPayslip.PerMinuteRate     what one minute cost: Gross / (days x hours x 60)
      HrPayslip.RateWorkingDays   the days it was divided by (month's working days)
      HrPayslip.RateHours         the hours of the working day it was divided by
      so the Payroll Review can show the rate and how it was reached. Payslips
      generated before this show 0 until the payroll is generated again.

   RE-GENERATE the month's payroll afterwards. Additive and safe to re-run.
   ============================================================================ */
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

UPDATE dbo.HrPayComponent SET CalcType = 8  WHERE Code = 'SHORTLEAVE' AND CalcType = 3;
UPDATE dbo.HrPayComponent SET CalcType = 9  WHERE Code = 'EARLY'      AND CalcType = 3;
UPDATE dbo.HrPayComponent SET CalcType = 10 WHERE Code = 'DUTYSWAP'   AND CalcType = 3;
GO

IF COL_LENGTH('dbo.HrPayslip', 'PerMinuteRate') IS NULL
    ALTER TABLE dbo.HrPayslip ADD PerMinuteRate DECIMAL(18,6) NOT NULL CONSTRAINT DF_HrPS_PerMinuteRate DEFAULT (0);
GO
IF COL_LENGTH('dbo.HrPayslip', 'RateWorkingDays') IS NULL
    ALTER TABLE dbo.HrPayslip ADD RateWorkingDays DECIMAL(5,2) NOT NULL CONSTRAINT DF_HrPS_RateWorkingDays DEFAULT (0);
GO
IF COL_LENGTH('dbo.HrPayslip', 'RateHours') IS NULL
    ALTER TABLE dbo.HrPayslip ADD RateHours DECIMAL(5,2) NOT NULL CONSTRAINT DF_HrPS_RateHours DEFAULT (0);
GO

SELECT 'Rule ' + Code + ' : CalcType ' + CAST(CalcType AS VARCHAR(5)) FROM dbo.HrPayComponent
 WHERE Code IN ('LATE', 'SHORTLEAVE', 'EARLY', 'DUTYSWAP') ORDER BY Code;
SELECT 'HrPayslip rate columns : ' + CASE WHEN COL_LENGTH('dbo.HrPayslip', 'RateHours') IS NULL THEN 'missing' ELSE 'ready' END;
GO

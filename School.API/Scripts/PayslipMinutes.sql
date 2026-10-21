/* ============================================================================
   Payslip minutes - early leaving and time out of school, kept on the payslip
   ============================================================================

   Deliberately NO "USE [EasySchoolDB]" here: a hard-coded USE overrides
   sqlcmd's -d. The database comes from -d instead.

     cd School.API\Scripts
     sqlcmd -S . -U sa -P <pwd> -d EasySchoolDB -i PayslipMinutes.sql

   A payslip already remembered the month's late minutes. Early leaving and
   time out of school were charged too, but their MINUTES were not kept, so
   the salary slip could show the deduction and not what it was for.

     HrPayslip.EarlyMinutes    minutes left before the day ended, in the month
     HrPayslip.OutingMinutes   minutes out of school during the day

   Payslips generated before this script show 0 until the payroll is
   generated again. Additive and safe to re-run.

   QUOTED_IDENTIFIER must be ON: HrPayslip references Employees, which
   carries a computed column.
   ============================================================================ */
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF COL_LENGTH('dbo.HrPayslip', 'EarlyMinutes') IS NULL
    ALTER TABLE dbo.HrPayslip ADD EarlyMinutes INT NOT NULL
        CONSTRAINT DF_HrPS_EarlyMinutes DEFAULT (0);
GO

IF COL_LENGTH('dbo.HrPayslip', 'OutingMinutes') IS NULL
    ALTER TABLE dbo.HrPayslip ADD OutingMinutes INT NOT NULL
        CONSTRAINT DF_HrPS_OutingMinutes DEFAULT (0);
GO

SELECT 'HrPayslip minutes : ' + CASE WHEN COL_LENGTH('dbo.HrPayslip', 'OutingMinutes') IS NULL THEN 'missing' ELSE 'ready' END;
GO

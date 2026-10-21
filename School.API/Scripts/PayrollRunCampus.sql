/* ============================================================================
   Payroll run per campus - a salary bill for each campus
   ============================================================================

   Deliberately NO "USE [EasySchoolDB]" here: a hard-coded USE overrides
   sqlcmd's -d. The database comes from -d instead.

     cd School.API\Scripts
     sqlcmd -S . -U sa -P <pwd> -d EasySchoolDB -i PayrollRunCampus.sql

   THE RULE
   --------
   A month was one payroll run for the whole school. It can now be one run
   per campus instead, each generated, reviewed, locked and paid on its own,
   so each campus gets its own salary bill.

     HrPayrollRun.CampusId   NULL  = all campuses together (as before)
                             a VID = that campus only

   A month is run one way or the other, never both: the application refuses
   an all-campus run while campus runs exist for the month, and the other way
   round, so nobody can be paid twice.

   WHAT THIS CHANGES

     HrPayrollRun.CampusId   new, NULL for every run that already exists, so
                             existing runs stay "all campuses" unchanged
     UX_HrPR_Period          (PayYear, PayMonth) - one run a month - dropped
     UX_HrPR_Period_Campus   (PayYear, PayMonth, CampusId) in its place. SQL
                             Server lets a unique index hold one NULL, so a
                             month still has at most one all-campus run, and
                             at most one run per campus.

   Additive and safe to re-run.
   ============================================================================ */
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF COL_LENGTH('dbo.HrPayrollRun', 'CampusId') IS NULL
    ALTER TABLE dbo.HrPayrollRun ADD CampusId INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_HrPR_Campus')
    ALTER TABLE dbo.HrPayrollRun
        ADD CONSTRAINT FK_HrPR_Campus FOREIGN KEY (CampusId) REFERENCES dbo.SMSCampus(VID);
GO

-- The new index first, then the old one goes: the table is never without a
-- rule against a second run for the same month.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_HrPR_Period_Campus' AND object_id = OBJECT_ID('dbo.HrPayrollRun'))
    CREATE UNIQUE INDEX UX_HrPR_Period_Campus ON dbo.HrPayrollRun (PayYear, PayMonth, CampusId);
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_HrPR_Period' AND object_id = OBJECT_ID('dbo.HrPayrollRun'))
    DROP INDEX UX_HrPR_Period ON dbo.HrPayrollRun;
GO

SELECT 'HrPayrollRun.CampusId : ' + CASE WHEN COL_LENGTH('dbo.HrPayrollRun', 'CampusId') IS NULL THEN 'missing' ELSE 'ready' END;
SELECT 'Run per campus index  : ' + CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_HrPR_Period_Campus') THEN 'ready' ELSE 'missing' END;
GO

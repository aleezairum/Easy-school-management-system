/* ============================================================================
   Extra days - paid on top of the salary for a month with no day off
   ============================================================================

   Deliberately NO "USE [EasySchoolDB]" here: a hard-coded USE overrides
   sqlcmd's -d. The database comes from -d instead.

     cd School.API\Scripts
     sqlcmd -S . -U sa -P <pwd> -d EasySchoolDB -i ExtraDays.sql

   THE RULE
   --------
   A member of staff qualifies for a month when BOTH hold:

     1. they took no day off in it - no leave, no absence, no half day, and
        no working day left unmarked; and
     2. the month has more working days than a threshold. The threshold is a
        SETTING, not a number in the code: 20 to start with, changeable from
        the Extra Days screen.

   Those who qualify are given a number of extra days, which differs by
   designation (a teacher might get 2, a guard 1). The screen remembers each
   designation's number so it does not have to be typed again every month,
   and any single person's figure can still be changed by hand.

   Each extra day is paid at the salary sheet's own Daily Pay - Basic divided
   by the days in the month - so the office can check Extra Days x Daily Pay
   against the same row of the printed sheet.

   WHAT THIS CREATES

     HrExtraDaysSetting   one row: the working-days threshold (default 20)
     HrExtraDaysRule      extra days per designation, remembered
     HrPayrollExtraDays   the extra days actually given, per person per month
     HrPayslip            ExtraDays and ExtraDaysPay, so a reprinted sheet
                          shows what was paid at the time

   QUOTED_IDENTIFIER must be ON: HrPayslip is altered here, and Employees
   (referenced by a foreign key) carries a computed column.
   ============================================================================ */
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* ── 1. The threshold - a setting, not a constant ───────────────────────── */

IF OBJECT_ID('dbo.HrExtraDaysSetting', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HrExtraDaysSetting
    (
        Id              INT          NOT NULL CONSTRAINT PK_HrExtraDaysSetting PRIMARY KEY,
        -- "More than this many working days in the month".
        MinWorkingDays  INT          NOT NULL CONSTRAINT DF_HrEDS_Min DEFAULT (20),
        UpdatedAt       DATETIME     NULL,
        UpdatedBy       INT          NULL,
        -- One row, always: the screen reads and writes Id 1.
        CONSTRAINT CK_HrExtraDaysSetting_OneRow CHECK (Id = 1)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.HrExtraDaysSetting WHERE Id = 1)
    INSERT dbo.HrExtraDaysSetting (Id, MinWorkingDays) VALUES (1, 20);
GO

/* ── 2. Extra days per designation, remembered between months ───────────── */

IF OBJECT_ID('dbo.HrExtraDaysRule', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HrExtraDaysRule
    (
        DesignationId   INT           NOT NULL CONSTRAINT PK_HrExtraDaysRule PRIMARY KEY,
        ExtraDays       DECIMAL(5,2)  NOT NULL CONSTRAINT DF_HrEDR_Days DEFAULT (0),
        UpdatedAt       DATETIME      NULL,
        UpdatedBy       INT           NULL,
        CONSTRAINT FK_HrEDR_Designation FOREIGN KEY (DesignationId) REFERENCES dbo.Designations (Id),
        CONSTRAINT CK_HrEDR_NotNegative CHECK (ExtraDays >= 0)
    );
END
GO

/* ── 3. What was actually given, per person per month ───────────────────── */

IF OBJECT_ID('dbo.HrPayrollExtraDays', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HrPayrollExtraDays
    (
        Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HrPayrollExtraDays PRIMARY KEY,
        EmployeeId  INT           NOT NULL,
        PayYear     INT           NOT NULL,
        PayMonth    TINYINT       NOT NULL,
        ExtraDays   DECIMAL(5,2)  NOT NULL,
        CreatedAt   DATETIME      NOT NULL CONSTRAINT DF_HrPED_Created DEFAULT (GETDATE()),
        CreatedBy   INT           NULL,
        UpdatedAt   DATETIME      NULL,
        UpdatedBy   INT           NULL,
        CONSTRAINT FK_HrPED_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees (Id),
        CONSTRAINT CK_HrPED_Positive CHECK (ExtraDays > 0)
    );

    CREATE UNIQUE INDEX UX_HrPED_Employee_Month
        ON dbo.HrPayrollExtraDays (PayYear, PayMonth, EmployeeId);
END
GO

/* ── 4. What a payslip remembers ────────────────────────────────────────── */

IF COL_LENGTH('dbo.HrPayslip', 'ExtraDays') IS NULL
    ALTER TABLE dbo.HrPayslip ADD ExtraDays DECIMAL(5,2) NOT NULL
        CONSTRAINT DF_HrPayslip_ExtraDays DEFAULT (0);
GO

IF COL_LENGTH('dbo.HrPayslip', 'ExtraDaysPay') IS NULL
    ALTER TABLE dbo.HrPayslip ADD ExtraDaysPay DECIMAL(18,2) NOT NULL
        CONSTRAINT DF_HrPayslip_ExtraDaysPay DEFAULT (0);
GO

SELECT 'HrExtraDaysSetting : ' + CASE WHEN OBJECT_ID('dbo.HrExtraDaysSetting') IS NULL THEN 'missing'
       ELSE 'ready, more than ' + CAST((SELECT MinWorkingDays FROM dbo.HrExtraDaysSetting WHERE Id = 1) AS VARCHAR)
            + ' working days' END;
SELECT 'HrExtraDaysRule    : ' + CASE WHEN OBJECT_ID('dbo.HrExtraDaysRule') IS NULL THEN 'missing' ELSE 'ready' END;
SELECT 'HrPayrollExtraDays : ' + CASE WHEN OBJECT_ID('dbo.HrPayrollExtraDays') IS NULL THEN 'missing' ELSE 'ready' END;
SELECT 'HrPayslip columns  : ' + CASE WHEN COL_LENGTH('dbo.HrPayslip','ExtraDaysPay') IS NULL THEN 'missing' ELSE 'ready' END;
GO

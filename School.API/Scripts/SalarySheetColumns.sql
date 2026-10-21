/* ============================================================================
   The school's own salary sheet - the columns it has always had on paper
   ============================================================================

   Deliberately NO "USE [EasySchoolDB]" here: a hard-coded USE overrides
   sqlcmd's -d. The database comes from -d instead.

     cd School.API\Scripts
     sqlcmd -S . -U sa -P <pwd> -d EasySchoolDB -i SalarySheetColumns.sql

   THE SHEET
   ---------
   SALARY OF THE STAFF FOR THE MONTH OF <month> <year>, seventeen columns:

     Sr | Name | Designation | Basic Pay | Total Days | Daily Pay |
     Working Days | No. of Leaves | Incentive | Security Return |
     Pay Deduction | Prize/Fine | Security Deduction | Net Pay |
     Advance | Take home salary | Signature

   The system could already produce nine of them. The other eight were missing
   for one reason: six of them are amounts that differ per person AND per
   month - one teacher's incentive was 23,375 while another's was 1,435 - and
   there was nowhere to type such a thing. The pay structure holds a basic
   salary with an effective date, not a figure that changes every month.

   WHAT THIS CREATES

     HrPayrollMonthlyAmount   one row per employee per month, holding the six
                              hand-entered amounts. Nothing computes these; the
                              office types them before running the payroll.

     Ten columns on HrPayslip  so a payslip, once generated, still says what it
                              was built from. A salary sheet reprinted in March
                              must show what March's amounts were, not what
                              they have since been changed to.

   THE THREE DAY COLUMNS, precisely:

     TotalDays    calendar days in the month (31 for August)
     WorkingDays  days this person was actually on the staff that month, so a
                  joiner mid-month shows fewer. A full month equals TotalDays,
                  which is what the school's sheet shows.
     DailyPay     Basic / TotalDays. 44,500 / 31 = 1,435.48, exactly as on the
                  sheet.

   These are for the sheet. They are NOT the divisor behind the per-minute
   late and early deductions, which stays what the school chose: the month's
   working days less public holidays and less each employee's weekly off.

   QUOTED_IDENTIFIER must be ON - HrPayslip and Employees are written here and
   Employees carries a computed column. It is set below.
   ============================================================================ */
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* ── 1. The month's hand-entered amounts ────────────────────────────────── */

IF OBJECT_ID('dbo.HrPayrollMonthlyAmount', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HrPayrollMonthlyAmount
    (
        Id                INT IDENTITY(1,1) NOT NULL
                          CONSTRAINT PK_HrPayrollMonthlyAmount PRIMARY KEY,
        EmployeeId        INT           NOT NULL,
        PayYear           INT           NOT NULL,
        PayMonth          TINYINT       NOT NULL,

        -- Added to the pay
        Incentive         DECIMAL(18,2) NOT NULL CONSTRAINT DF_HrPMA_Incentive      DEFAULT (0),
        SecurityReturn    DECIMAL(18,2) NOT NULL CONSTRAINT DF_HrPMA_SecurityReturn DEFAULT (0),

        -- Taken off the pay
        PayDeduction      DECIMAL(18,2) NOT NULL CONSTRAINT DF_HrPMA_PayDeduction   DEFAULT (0),
        SecurityDeduction DECIMAL(18,2) NOT NULL CONSTRAINT DF_HrPMA_SecurityDed    DEFAULT (0),

        -- Advance already handed over, taken off AFTER the net pay so the sheet
        -- can show both "Net Pay" and "Take home salary" the way it always has.
        Advance           DECIMAL(18,2) NOT NULL CONSTRAINT DF_HrPMA_Advance        DEFAULT (0),

        /* SIGNED, on purpose. The school's sheet writes a fine as a negative
           number (-583) and totals the column to -4,071, so the sign is the
           office's own shorthand: minus is money taken, plus is a prize given.
           Storing it as typed keeps the sheet and the screen identical. */
        PrizeFine         DECIMAL(18,2) NOT NULL CONSTRAINT DF_HrPMA_PrizeFine      DEFAULT (0),

        Remarks           NVARCHAR(255) NULL,
        CreatedAt         DATETIME      NOT NULL CONSTRAINT DF_HrPMA_Created DEFAULT (GETDATE()),
        CreatedBy         INT           NULL,
        UpdatedAt         DATETIME      NULL,
        UpdatedBy         INT           NULL,

        CONSTRAINT FK_HrPMA_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees (Id)
    );

    -- One row per person per month, and the payroll run looks them up by
    -- month, so this is both the rule and the lookup.
    CREATE UNIQUE INDEX UX_HrPMA_Employee_Month
        ON dbo.HrPayrollMonthlyAmount (PayYear, PayMonth, EmployeeId);
END
GO

/* ── 2. What a payslip remembers ────────────────────────────────────────── */

DECLARE @cols TABLE (name SYSNAME, def NVARCHAR(200));
INSERT @cols VALUES
    ('TotalDays',         'DECIMAL(9,2) NOT NULL DEFAULT (0)'),
    ('WorkingDays',       'DECIMAL(9,2) NOT NULL DEFAULT (0)'),
    ('DailyPay',          'DECIMAL(18,2) NOT NULL DEFAULT (0)'),
    ('Incentive',         'DECIMAL(18,2) NOT NULL DEFAULT (0)'),
    ('SecurityReturn',    'DECIMAL(18,2) NOT NULL DEFAULT (0)'),
    ('PayDeduction',      'DECIMAL(18,2) NOT NULL DEFAULT (0)'),
    ('PrizeFine',         'DECIMAL(18,2) NOT NULL DEFAULT (0)'),
    ('SecurityDeduction', 'DECIMAL(18,2) NOT NULL DEFAULT (0)'),
    ('Advance',           'DECIMAL(18,2) NOT NULL DEFAULT (0)'),
    ('TakeHome',          'DECIMAL(18,2) NOT NULL DEFAULT (0)');

DECLARE @name SYSNAME, @def NVARCHAR(200), @sql NVARCHAR(400);
DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT name, def FROM @cols;
OPEN c;
FETCH NEXT FROM c INTO @name, @def;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns
                   WHERE object_id = OBJECT_ID('dbo.HrPayslip') AND name = @name)
    BEGIN
        SET @sql = 'ALTER TABLE dbo.HrPayslip ADD ' + QUOTENAME(@name) + ' ' + @def + ';';
        EXEC sp_executesql @sql;
    END
    FETCH NEXT FROM c INTO @name, @def;
END
CLOSE c; DEALLOCATE c;
GO

SELECT 'HrPayrollMonthlyAmount: ' +
       CASE WHEN OBJECT_ID('dbo.HrPayrollMonthlyAmount') IS NULL THEN 'missing' ELSE 'ready' END;

SELECT 'HrPayslip new columns : ' + CAST(COUNT(*) AS VARCHAR) + ' of 10'
FROM   sys.columns
WHERE  object_id = OBJECT_ID('dbo.HrPayslip')
  AND  name IN ('TotalDays','WorkingDays','DailyPay','Incentive','SecurityReturn',
                'PayDeduction','PrizeFine','SecurityDeduction','Advance','TakeHome');
GO

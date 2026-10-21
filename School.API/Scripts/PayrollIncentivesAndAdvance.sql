/* ============================================================================
   Payroll: incentives itemised, Prize and Fine apart, staff advances by date
   ----------------------------------------------------------------------------
   Run on the target database:
     sqlcmd -S . -U sa -P <pwd> -d EasySchoolDB -i PayrollIncentivesAndAdvance.sql

   HrPayrollMonthlyAmount and HrPayslip get four columns:
     ExtraDuty, AcademyDuty   incentives typed per month (Monthly Amounts),
                              added to the pay like Security Return
     Prize, Fine              in their own columns. Until now one signed
                              PrizeFine column held both (minus = fine); it is
                              split once here and kept as Prize - Fine.

   HrStaffAdvance   advances handed to staff, one row per advance with its
                    date (Staff & Payroll -> Staff Advances). A month's payroll
                    takes the advances dated in that month. The Advance typed
                    on Monthly Amounts until now is carried into it once,
                    dated the 1st of its month.

   Additive and safe to run twice: columns and the table are added only when
   missing, and each one-time copy runs only while its target is empty.
   ============================================================================ */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ── Monthly Amounts ─────────────────────────────────────────────────────────
IF COL_LENGTH('dbo.HrPayrollMonthlyAmount', 'ExtraDuty') IS NULL
    ALTER TABLE dbo.HrPayrollMonthlyAmount ADD ExtraDuty DECIMAL(18,2) NOT NULL CONSTRAINT DF_HrPMA_ExtraDuty DEFAULT 0;
IF COL_LENGTH('dbo.HrPayrollMonthlyAmount', 'AcademyDuty') IS NULL
    ALTER TABLE dbo.HrPayrollMonthlyAmount ADD AcademyDuty DECIMAL(18,2) NOT NULL CONSTRAINT DF_HrPMA_AcademyDuty DEFAULT 0;
IF COL_LENGTH('dbo.HrPayrollMonthlyAmount', 'Prize') IS NULL
    ALTER TABLE dbo.HrPayrollMonthlyAmount ADD Prize DECIMAL(18,2) NOT NULL CONSTRAINT DF_HrPMA_Prize DEFAULT 0;
IF COL_LENGTH('dbo.HrPayrollMonthlyAmount', 'Fine') IS NULL
    ALTER TABLE dbo.HrPayrollMonthlyAmount ADD Fine DECIMAL(18,2) NOT NULL CONSTRAINT DF_HrPMA_Fine DEFAULT 0;
GO

-- ── Payslip ─────────────────────────────────────────────────────────────────
IF COL_LENGTH('dbo.HrPayslip', 'ExtraDuty') IS NULL
    ALTER TABLE dbo.HrPayslip ADD ExtraDuty DECIMAL(18,2) NOT NULL CONSTRAINT DF_HrPayslip_ExtraDuty DEFAULT 0;
IF COL_LENGTH('dbo.HrPayslip', 'AcademyDuty') IS NULL
    ALTER TABLE dbo.HrPayslip ADD AcademyDuty DECIMAL(18,2) NOT NULL CONSTRAINT DF_HrPayslip_AcademyDuty DEFAULT 0;
IF COL_LENGTH('dbo.HrPayslip', 'Prize') IS NULL
    ALTER TABLE dbo.HrPayslip ADD Prize DECIMAL(18,2) NOT NULL CONSTRAINT DF_HrPayslip_Prize DEFAULT 0;
IF COL_LENGTH('dbo.HrPayslip', 'Fine') IS NULL
    ALTER TABLE dbo.HrPayslip ADD Fine DECIMAL(18,2) NOT NULL CONSTRAINT DF_HrPayslip_Fine DEFAULT 0;
GO

-- The old signed PrizeFine, split once. Only rows not split yet (both new
-- columns still 0) are touched, so a second run changes nothing.
UPDATE dbo.HrPayrollMonthlyAmount
   SET Prize = CASE WHEN PrizeFine > 0 THEN PrizeFine ELSE 0 END,
       Fine  = CASE WHEN PrizeFine < 0 THEN -PrizeFine ELSE 0 END
 WHERE PrizeFine <> 0 AND Prize = 0 AND Fine = 0;

UPDATE dbo.HrPayslip
   SET Prize = CASE WHEN PrizeFine > 0 THEN PrizeFine ELSE 0 END,
       Fine  = CASE WHEN PrizeFine < 0 THEN -PrizeFine ELSE 0 END
 WHERE PrizeFine <> 0 AND Prize = 0 AND Fine = 0;
GO

-- ── Staff advances ──────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.HrStaffAdvance', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HrStaffAdvance
    (
        Id           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HrStaffAdvance PRIMARY KEY,
        EmployeeId   INT            NOT NULL,
        AdvanceDate  DATE           NOT NULL,
        Amount       DECIMAL(18,2)  NOT NULL,
        Remarks      NVARCHAR(255)  NULL,
        CreatedAt    DATETIME2      NOT NULL CONSTRAINT DF_HrStaffAdvance_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy    INT            NULL,
        UpdatedAt    DATETIME2      NULL,
        UpdatedBy    INT            NULL,
        CONSTRAINT CK_HrStaffAdvance_Amount CHECK (Amount > 0)
    );
    CREATE INDEX IX_HrStaffAdvance_Employee_Date ON dbo.HrStaffAdvance (EmployeeId, AdvanceDate);

    -- What Monthly Amounts held until now, carried over once (only here, when
    -- the table is first made), dated the 1st of its month.
    INSERT dbo.HrStaffAdvance (EmployeeId, AdvanceDate, Amount, Remarks, CreatedAt, CreatedBy)
    SELECT EmployeeId, DATEFROMPARTS(PayYear, PayMonth, 1), Advance, N'From Monthly Amounts', SYSUTCDATETIME(), CreatedBy
      FROM dbo.HrPayrollMonthlyAmount
     WHERE Advance > 0;
END
GO

SELECT 'Payroll incentives : ' + CASE WHEN COL_LENGTH('dbo.HrPayslip', 'AcademyDuty') IS NULL THEN 'missing' ELSE 'ready' END;
SELECT 'Staff advances     : ' + CASE WHEN OBJECT_ID('dbo.HrStaffAdvance') IS NULL THEN 'missing' ELSE 'ready' END;

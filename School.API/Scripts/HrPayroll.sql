-- ============================================================================
-- HR & Payroll module — idempotent schema script.
-- Safe to re-run. Run manually against EasySchoolDB (no EF migrations here,
-- consistent with the rest of Scripts/).
-- ============================================================================

-- Required by the filtered indexes below (WHERE clauses on HrHoliday, HrAttendance, HrLeave).
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

------------------------------------------------------------------ Shift
IF OBJECT_ID('HrShift','U') IS NULL
CREATE TABLE HrShift (
    Id             INT IDENTITY(1,1) PRIMARY KEY,
    Code           NVARCHAR(20)  NOT NULL,
    Name           NVARCHAR(100) NOT NULL,
    TimeIn         TIME(0)       NOT NULL,
    TimeOut        TIME(0)       NOT NULL,
    GraceMinutes   INT           NOT NULL CONSTRAINT DF_HrShift_Grace DEFAULT 0,
    WorkHours      DECIMAL(5,2)  NOT NULL,
    HalfDayMinutes INT           NOT NULL CONSTRAINT DF_HrShift_HD DEFAULT 0,
    FullDayMinutes INT           NOT NULL CONSTRAINT DF_HrShift_FD DEFAULT 0,
    OffDay1        TINYINT       NULL,   -- 1=Sun .. 7=Sat
    OffDay2        TINYINT       NULL,
    EffectiveFrom  DATE          NULL,   -- summer / winter timings
    EffectiveTo    DATE          NULL,
    IsActive       BIT           NOT NULL CONSTRAINT DF_HrShift_Act DEFAULT 1,
    CreatedAt      DATETIME2(0)  NOT NULL CONSTRAINT DF_HrShift_CA DEFAULT SYSUTCDATETIME(),
    UpdatedAt      DATETIME2(0)  NULL
);
GO

------------------------------------------------------------------ Holiday
IF OBJECT_ID('HrHoliday','U') IS NULL
CREATE TABLE HrHoliday (
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    HDate     DATE          NOT NULL,
    Name      NVARCHAR(150) NOT NULL,
    HType     TINYINT       NOT NULL,   -- 1=Gazetted 2=Vacation 3=HalfDay
    IsActive  BIT           NOT NULL CONSTRAINT DF_HrHoliday_Act DEFAULT 1
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_HrHoliday_Date')
CREATE UNIQUE INDEX UX_HrHoliday_Date ON HrHoliday(HDate) WHERE IsActive = 1;
GO

------------------------------------------------------------------ Leave type (seeded, no CRUD screen)
IF OBJECT_ID('HrLeaveType','U') IS NULL
CREATE TABLE HrLeaveType (
    Id         INT IDENTITY(1,1) PRIMARY KEY,
    Code       NVARCHAR(20)  NOT NULL,
    Name       NVARCHAR(100) NOT NULL,
    IsPaid     BIT           NOT NULL CONSTRAINT DF_HrLT_Paid DEFAULT 1,
    DeductDays DECIMAL(4,2)  NOT NULL CONSTRAINT DF_HrLT_Ded  DEFAULT 0,
    MaxPerYear INT           NULL,
    IsActive   BIT           NOT NULL CONSTRAINT DF_HrLT_Act   DEFAULT 1
);
GO
IF NOT EXISTS (SELECT 1 FROM HrLeaveType WHERE Code = 'CASUAL')
INSERT INTO HrLeaveType (Code, Name, IsPaid, DeductDays, MaxPerYear) VALUES
    ('CASUAL', 'Casual Leave', 1, 1, 10),
    ('SICK',   'Sick Leave',   1, 1, 8),
    ('ANNUAL', 'Annual Leave', 1, 1, 14),
    ('UNPAID', 'Leave Without Pay', 0, 1, NULL);
GO

------------------------------------------------------------------ Attendance  (one row / employee / day)
IF OBJECT_ID('HrAttendance','U') IS NULL
CREATE TABLE HrAttendance (
    Id            BIGINT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId    INT           NOT NULL,
    AttDate       DATE          NOT NULL,
    ShiftId       INT           NULL,
    TimeIn        TIME(0)       NULL,
    TimeOut       TIME(0)       NULL,
    Status        TINYINT       NOT NULL,   -- 1=Present 2=Absent 3=Leave 4=HalfDay
                                            -- 5=Holiday 6=WeeklyOff 7=ShortLeave
    LateMinutes   INT           NOT NULL CONSTRAINT DF_HrAtt_Late  DEFAULT 0,
    EarlyMinutes  INT           NOT NULL CONSTRAINT DF_HrAtt_Early DEFAULT 0,
    WorkedMinutes INT           NOT NULL CONSTRAINT DF_HrAtt_Work  DEFAULT 0,
    LeaveTypeId   INT           NULL,
    IgnoreLate    BIT           NOT NULL CONSTRAINT DF_HrAtt_IgnL  DEFAULT 0,
    Source        TINYINT       NOT NULL CONSTRAINT DF_HrAtt_Src   DEFAULT 1, -- 1=Manual 2=Import
    ImportBatchId UNIQUEIDENTIFIER NULL,
    Remarks       NVARCHAR(300) NULL,
    CreatedBy     INT           NULL,
    CreatedAt     DATETIME2(0)  NOT NULL CONSTRAINT DF_HrAtt_CA DEFAULT SYSUTCDATETIME(),
    UpdatedBy     INT           NULL,
    UpdatedAt     DATETIME2(0)  NULL,
    CONSTRAINT FK_HrAtt_Emp   FOREIGN KEY (EmployeeId)  REFERENCES Employees(Id),
    CONSTRAINT FK_HrAtt_Shift FOREIGN KEY (ShiftId)     REFERENCES HrShift(Id),
    CONSTRAINT FK_HrAtt_Lv    FOREIGN KEY (LeaveTypeId) REFERENCES HrLeaveType(Id)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_HrAtt_Emp_Date')
CREATE UNIQUE INDEX UX_HrAtt_Emp_Date ON HrAttendance(EmployeeId, AttDate);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_HrAtt_Date')
CREATE INDEX IX_HrAtt_Date ON HrAttendance(AttDate)
    INCLUDE (EmployeeId, Status, LateMinutes, WorkedMinutes);
GO

------------------------------------------------------------------ Leave
IF OBJECT_ID('HrLeave','U') IS NULL
CREATE TABLE HrLeave (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId  INT           NOT NULL,
    LeaveTypeId INT           NOT NULL,
    FromDate    DATE          NOT NULL,
    ToDate      DATE          NOT NULL,
    Days        DECIMAL(5,2)  NOT NULL,
    Status      TINYINT       NOT NULL CONSTRAINT DF_HrLv_St DEFAULT 1, -- 1=Pending 2=Approved 3=Rejected
    Reason      NVARCHAR(300) NULL,
    ApprovedBy  INT           NULL,
    ApprovedAt  DATETIME2(0)  NULL,
    CreatedAt   DATETIME2(0)  NOT NULL CONSTRAINT DF_HrLv_CA DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_HrLv_Emp FOREIGN KEY (EmployeeId)  REFERENCES Employees(Id),
    CONSTRAINT FK_HrLv_Lv  FOREIGN KEY (LeaveTypeId) REFERENCES HrLeaveType(Id)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_HrLv_Emp_Date')
CREATE INDEX IX_HrLv_Emp_Date ON HrLeave(EmployeeId, FromDate, ToDate) WHERE Status = 2;
GO

------------------------------------------------------------------ Pay component  (the whole rule engine)
IF OBJECT_ID('HrPayComponent','U') IS NULL
CREATE TABLE HrPayComponent (
    Id                INT IDENTITY(1,1) PRIMARY KEY,
    Code              NVARCHAR(30)   NOT NULL,
    Name              NVARCHAR(100)  NOT NULL,
    Kind              TINYINT        NOT NULL,   -- 1=Earning 2=Deduction
    CalcType          TINYINT        NOT NULL,   -- 1=Fixed 2=PercentOf 3=PerLateMinute
                                                 -- 4=PerAbsentDay 5=PerOvertimeHour 6=Slab 7=Formula
    Rate              DECIMAL(18,4)  NULL,
    RateSource        TINYINT        NOT NULL CONSTRAINT DF_HrPC_RS DEFAULT 1, -- 1=Fixed 2=Derived
    BaseOn            TINYINT        NULL,       -- 1=Basic 2=Gross
    DivisorDays       TINYINT        NULL,       -- 30 | 0=Calendar | 255=WorkingDays
    DivisorHours      DECIMAL(5,2)   NULL,       -- NULL = take from shift
    GraceMinutes      INT            NULL,
    FreeUnitsPerMonth DECIMAL(9,2)   NULL,
    CapPerDay         DECIMAL(18,2)  NULL,
    CapPerMonth       DECIMAL(18,2)  NULL,
    SlabsJson         NVARCHAR(MAX)  NULL,
    Formula           NVARCHAR(500)  NULL,
    ScopeType         TINYINT        NOT NULL CONSTRAINT DF_HrPC_Sc DEFAULT 1, -- 1=Global 2=Grade
                                                 -- 3=Designation 4=Employee
    ScopeId           INT            NULL,
    Priority          INT            NOT NULL CONSTRAINT DF_HrPC_Pr DEFAULT 0,
    ShowOnPayslip     BIT            NOT NULL CONSTRAINT DF_HrPC_Sh DEFAULT 1,
    EffectiveFrom     DATE           NOT NULL,
    EffectiveTo       DATE           NULL,
    IsActive          BIT            NOT NULL CONSTRAINT DF_HrPC_Act DEFAULT 1
);
GO

------------------------------------------------------------------ Employee salary structure
IF OBJECT_ID('HrEmployeePay','U') IS NULL
CREATE TABLE HrEmployeePay (
    Id             INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId     INT            NOT NULL,
    EffectiveFrom  DATE           NOT NULL,
    Basic          DECIMAL(18,2)  NOT NULL,
    ComponentsJson NVARCHAR(MAX)  NULL,   -- [{componentId, amount}]
    IsActive       BIT            NOT NULL CONSTRAINT DF_HrEP_Act DEFAULT 1,
    CreatedAt      DATETIME2(0)   NOT NULL CONSTRAINT DF_HrEP_CA DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_HrEP_Emp FOREIGN KEY (EmployeeId) REFERENCES Employees(Id)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_HrEP_Emp_Eff')
CREATE INDEX IX_HrEP_Emp_Eff ON HrEmployeePay(EmployeeId, EffectiveFrom DESC);
GO

------------------------------------------------------------------ Payroll run
IF OBJECT_ID('HrPayrollRun','U') IS NULL
CREATE TABLE HrPayrollRun (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    PayYear     INT          NOT NULL,
    PayMonth    TINYINT      NOT NULL,
    Status      TINYINT      NOT NULL CONSTRAINT DF_HrPR_St DEFAULT 1, -- 1=Draft 2=Locked 3=Paid
    GeneratedBy INT          NULL,
    GeneratedAt DATETIME2(0) NULL,
    LockedBy    INT          NULL,
    LockedAt    DATETIME2(0) NULL
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_HrPR_Period')
CREATE UNIQUE INDEX UX_HrPR_Period ON HrPayrollRun(PayYear, PayMonth);
GO

------------------------------------------------------------------ Payslip
IF OBJECT_ID('HrPayslip','U') IS NULL
CREATE TABLE HrPayslip (
    Id             INT IDENTITY(1,1) PRIMARY KEY,
    RunId          INT            NOT NULL,
    EmployeeId     INT            NOT NULL,
    Basic          DECIMAL(18,2)  NOT NULL,
    GrossEarning   DECIMAL(18,2)  NOT NULL,
    TotalDeduction DECIMAL(18,2)  NOT NULL,
    NetPay         DECIMAL(18,2)  NOT NULL,
    PresentDays    DECIMAL(5,2)   NOT NULL,
    AbsentDays     DECIMAL(5,2)   NOT NULL,
    LeaveDays      DECIMAL(5,2)   NOT NULL,
    LateMinutes    INT            NOT NULL,
    BreakdownJson  NVARCHAR(MAX)  NULL,   -- [{componentId, name, kind, amount}]
    PaidDate       DATE           NULL,
    PaymentMethod  NVARCHAR(30)   NULL,
    Reference      NVARCHAR(100)  NULL,
    CONSTRAINT FK_HrPS_Run FOREIGN KEY (RunId)      REFERENCES HrPayrollRun(Id),
    CONSTRAINT FK_HrPS_Emp FOREIGN KEY (EmployeeId) REFERENCES Employees(Id)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_HrPS_Run_Emp')
CREATE UNIQUE INDEX UX_HrPS_Run_Emp ON HrPayslip(RunId, EmployeeId);
GO

------------------------------------------------------------------ Employee: new columns
IF COL_LENGTH('Employees','ShiftId')       IS NULL ALTER TABLE Employees ADD ShiftId INT NULL;
IF COL_LENGTH('Employees','MachineCardNo') IS NULL ALTER TABLE Employees ADD MachineCardNo NVARCHAR(50) NULL;
IF COL_LENGTH('Employees','OffDay1')       IS NULL ALTER TABLE Employees ADD OffDay1 TINYINT NULL;
IF COL_LENGTH('Employees','OffDay2')       IS NULL ALTER TABLE Employees ADD OffDay2 TINYINT NULL;

-- Employees.TeacherId linked the HR row to its SMSTeacher row (D6). It only
-- makes sense while SMSTeacher is still a separate TABLE: once
-- RenameTeacherToEmployee.sql has folded the two together, Employees.Id IS the
-- academic id and this column has nothing left to point at — so on a migrated
-- database both the column and its FK are skipped.
IF OBJECT_ID('SMSTeacher','U') IS NOT NULL AND COL_LENGTH('Employees','TeacherId') IS NULL
    ALTER TABLE Employees ADD TeacherId INT NULL;  -- links to SMSTeacher.VID, D6
GO

IF OBJECT_ID('SMSTeacher','U') IS NOT NULL
   AND COL_LENGTH('Employees','TeacherId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Employees_SMSTeacher')
    ALTER TABLE Employees ADD CONSTRAINT FK_Employees_SMSTeacher FOREIGN KEY (TeacherId) REFERENCES SMSTeacher(VID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Employees_HrShift')
    ALTER TABLE Employees ADD CONSTRAINT FK_Employees_HrShift FOREIGN KEY (ShiftId) REFERENCES HrShift(Id);
GO

------------------------------------------------------------------ Employees: drop the dead flat salary columns
-- Superseded by HrEmployeePay. Nothing reads these (the old repository was
-- empty scaffolding — see HR-Payroll-Final-Plan.md section 1).
IF COL_LENGTH('Employees','BasicSalary') IS NOT NULL ALTER TABLE Employees DROP COLUMN BasicSalary;
IF COL_LENGTH('Employees','Allowances')  IS NOT NULL ALTER TABLE Employees DROP COLUMN Allowances;
IF COL_LENGTH('Employees','Deductions')  IS NOT NULL ALTER TABLE Employees DROP COLUMN Deductions;
IF COL_LENGTH('Employees','NetSalary')   IS NOT NULL ALTER TABLE Employees DROP COLUMN NetSalary;
GO

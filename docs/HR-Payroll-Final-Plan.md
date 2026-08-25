# HR & Payroll Module — Final Implementation Plan

**Project:** Easy School Management System (School.API + School.Web)
**Client:** Sir Syed Model School
**Date:** 2026-08-25
**Status:** Approved direction — build from this document.

> **Supersedes** `HR-Payroll-Module-Plan.md` (Plan A, the Dropware-shaped port) and
> `HR-Payroll-Lean-Plan.md` (Plan B, the lean proposal). Those two remain in `docs/` for the reasoning
> and the cut-list; **this file is the build spec.**

---

## 1. Starting position

The existing HR code is non-functional scaffolding, verified file by file:

| File | Reality |
|---|---|
| `Repositories/Implementations/HR/EmployeeRepository.cs` | 233 lines, 202 commented — empty class, zero methods |
| `Repositories/Interfaces/HR/IEmployeeRepository.cs` | Empty interface |
| `Controllers/EmployeeController.cs` | Zero endpoints |
| `Services/Implementations/HR/EmployeeService.cs` | 132 lines, 109 commented |
| `Controllers/SalaryController.cs` | Zero endpoints |
| `Services/Implementations/HR/SalaryService.cs` | 67 lines, 41 commented |

Live and worth keeping: **Designation**, **HRGrade**, **Teacher** (`SMSTeacher`).
Everything else in HR is deleted and rebuilt. No migration burden.

**Sizing:** ~200 staff × 31 days = ~6,200 attendance rows/month, ~74,000/year, ~200 payslips/month.
Volume is a non-issue. The plan optimises for few moving parts and correctness, not throughput.

---

## 2. Locked decisions

| # | Decision |
|---|---|
| **D1** | **Compute on save.** Late/early/worked minutes calculated in the service when attendance is saved. No posting run, no intermediate daily table, no posting SP. |
| **D2** | **Rules in C#, reports in SQL.** `PayrollCalculator` is a pure, unit-tested C# function. Reports stay raw SQL/SPs in `School.API/Scripts/`. |
| **D3** | **JSON columns** (EF Core 8 `OwnsMany(...).ToJson()`) for rule slabs, salary structure lines and payslip breakdowns — three child tables removed. |
| **D4** | **Late exemption is a flag** (`IgnoreLate`) on the attendance row, not a table + form. |
| **D5** | **Aggregate punches at import.** Hikvision export reduced to first-in/last-out at import time; no raw punch table. |
| **D6** | **Teachers stay in `SMSTeacher`.** See below — this was previously mis-scoped. |

### D6 in detail (corrected)

Earlier drafts said "merge `SMSTeacher` into `Employees` now." **That was wrong.** `SMSTeacher` is joined by:

- `TimeTable.TeacherId`
- `StaffSubject.TeacherId` (via `SpGet_SMSStaffSubject`, `SpSave_SMSStaffSubject`)
- **Four exam report stored procedures**: `SpRPT_SMSExamExecutive`, `SpRPT_SMSExamSubjectWise`,
  `SpRPT_SMSExamTeacherComparison`, `SpRPT_SMSExamTeacherWise`

A merge means rewriting four report SPs plus two academic modules — days of work and real regression risk,
for no payroll benefit.

**Instead:** add one nullable column, `Employees.TeacherId INT NULL`, linking an employee to their academic
teacher record. Payroll iterates `Employees`. Academic reports keep running off `SMSTeacher`, untouched.
A teacher who draws salary gets both rows, linked. A non-teaching employee has `TeacherId = NULL`.

Cost: one column, one dropdown on the Employee form, zero SP changes. **This removes the last blocking
question — nothing now blocks Phase 1.**

---

## 3. Data model

9 tables. All DDL is idempotent; ship as `School.API/Scripts/HrPayroll.sql`.

```sql
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

------------------------------------------------------------------ Holiday
IF OBJECT_ID('HrHoliday','U') IS NULL
CREATE TABLE HrHoliday (
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    HDate     DATE          NOT NULL,
    Name      NVARCHAR(150) NOT NULL,
    HType     TINYINT       NOT NULL,   -- 1=Gazetted 2=Vacation 3=HalfDay
    IsActive  BIT           NOT NULL CONSTRAINT DF_HrHoliday_Act DEFAULT 1
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_HrHoliday_Date')
CREATE UNIQUE INDEX UX_HrHoliday_Date ON HrHoliday(HDate) WHERE IsActive = 1;

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
    CONSTRAINT FK_HrAtt_Emp FOREIGN KEY (EmployeeId) REFERENCES Employees(Id)
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_HrAtt_Emp_Date')
CREATE UNIQUE INDEX UX_HrAtt_Emp_Date ON HrAttendance(EmployeeId, AttDate);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_HrAtt_Date')
CREATE INDEX IX_HrAtt_Date ON HrAttendance(AttDate)
    INCLUDE (EmployeeId, Status, LateMinutes, WorkedMinutes);

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
    CONSTRAINT FK_HrLv_Emp FOREIGN KEY (EmployeeId) REFERENCES Employees(Id)
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_HrLv_Emp_Date')
CREATE INDEX IX_HrLv_Emp_Date ON HrLeave(EmployeeId, FromDate, ToDate) WHERE Status = 2;

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
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_HrEP_Emp_Eff')
CREATE INDEX IX_HrEP_Emp_Eff ON HrEmployeePay(EmployeeId, EffectiveFrom DESC);

------------------------------------------------------------------ Payroll run
IF OBJECT_ID('HrPayrollRun','U') IS NULL
CREATE TABLE HrPayrollRun (
    Id         INT IDENTITY(1,1) PRIMARY KEY,
    PayYear    INT          NOT NULL,
    PayMonth   TINYINT      NOT NULL,
    Status     TINYINT      NOT NULL CONSTRAINT DF_HrPR_St DEFAULT 1, -- 1=Draft 2=Locked 3=Paid
    GeneratedBy INT         NULL,
    GeneratedAt DATETIME2(0) NULL,
    LockedBy   INT          NULL,
    LockedAt   DATETIME2(0) NULL
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_HrPR_Period')
CREATE UNIQUE INDEX UX_HrPR_Period ON HrPayrollRun(PayYear, PayMonth);

------------------------------------------------------------------ Payslip
IF OBJECT_ID('HrPayslip','U') IS NULL
CREATE TABLE HrPayslip (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    RunId         INT            NOT NULL,
    EmployeeId    INT            NOT NULL,
    Basic         DECIMAL(18,2)  NOT NULL,
    GrossEarning  DECIMAL(18,2)  NOT NULL,
    TotalDeduction DECIMAL(18,2) NOT NULL,
    NetPay        DECIMAL(18,2)  NOT NULL,
    PresentDays   DECIMAL(5,2)   NOT NULL,
    AbsentDays    DECIMAL(5,2)   NOT NULL,
    LeaveDays     DECIMAL(5,2)   NOT NULL,
    LateMinutes   INT            NOT NULL,
    BreakdownJson NVARCHAR(MAX)  NULL,   -- [{componentId, name, kind, amount}]
    PaidDate      DATE           NULL,
    PaymentMethod NVARCHAR(30)   NULL,
    Reference     NVARCHAR(100)  NULL,
    CONSTRAINT FK_HrPS_Run FOREIGN KEY (RunId) REFERENCES HrPayrollRun(Id)
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_HrPS_Run_Emp')
CREATE UNIQUE INDEX UX_HrPS_Run_Emp ON HrPayslip(RunId, EmployeeId);

------------------------------------------------------------------ Employee: 5 new columns
IF COL_LENGTH('Employees','ShiftId')       IS NULL ALTER TABLE Employees ADD ShiftId INT NULL;
IF COL_LENGTH('Employees','MachineCardNo') IS NULL ALTER TABLE Employees ADD MachineCardNo NVARCHAR(50) NULL;
IF COL_LENGTH('Employees','OffDay1')       IS NULL ALTER TABLE Employees ADD OffDay1 TINYINT NULL;
IF COL_LENGTH('Employees','OffDay2')       IS NULL ALTER TABLE Employees ADD OffDay2 TINYINT NULL;
IF COL_LENGTH('Employees','TeacherId')     IS NULL ALTER TABLE Employees ADD TeacherId INT NULL;  -- D6
```

**Dropped from `Employee`:** `BasicSalary`, `Allowances`, `Deductions`, `NetSalary` — superseded by
`HrEmployeePay`. Nothing reads them today (the repository is empty), so removal is safe.

**Documented constraint:** `TimeIn`/`TimeOut` are `TIME`, so a shift crossing midnight is not supported.
Deliberate — a school has no night shift. Revisit only if that changes.

---

## 4. Rule engine

### 4.1 Resolution
At payroll time, for each employee, select applicable `HrPayComponent` rows where
`EffectiveFrom <= MonthEnd AND (EffectiveTo IS NULL OR EffectiveTo >= MonthStart)` and `IsActive = 1`,
scoped **most-specific-wins**:

```
Employee (4)  >  Designation (3)  >  Grade (2)  >  Global (1)      then by Priority
```

### 4.2 Sir Syed configuration — one row
```
Code=LATE  Kind=Deduction  CalcType=PerLateMinute  RateSource=Derived  BaseOn=Basic
DivisorDays=30  DivisorHours=NULL(from shift)  GraceMinutes=10  CapPerDay=500  ScopeType=Global
```
```
perMinuteRate = Basic / 30 / ShiftWorkHours / 60
deduction     = MIN( MAX(0, LateMinutes - 10) * perMinuteRate , 500 )
```

### 4.3 Other schools — data changes only
| Requirement | Change |
|---|---|
| Flat Rs 20 per late minute | `RateSource=Fixed, Rate=20` |
| First 3 lates free, then half day | `CalcType=Slab, FreeUnitsPerMonth=3`, one slab row `Days=0.5` |
| Rs 500 flat per late day | `CalcType=Fixed, Rate=500` |
| No late deduction at all | `IsActive=0` |

`CalcType=Formula` (NCalc, add `NCalc` package) is the escape hatch. Variables: `LateMinutes`,
`LateCount`, `AbsentDays`, `WorkedMinutes`, `Basic`, `Gross`, `MonthDays`, `WorkingDays`,
`HoursPerDay`, `PerDayRate`, `PerMinuteRate`. Example:
`if(LateCount > 3, PerDayRate * 0.5, LateMinutes * PerMinuteRate)`

---

## 5. Calculation spec

### 5.1 On attendance save — `AttendanceCalculator` (pure, no DB)

```
input : shift, timeIn, timeOut, status, isHoliday, isWeeklyOff, approvedLeave
output: status, lateMinutes, earlyMinutes, workedMinutes

if isHoliday        -> Status=Holiday,    all zero, return
if isWeeklyOff      -> Status=WeeklyOff,  all zero, return
if approvedLeave    -> Status=Leave, LeaveTypeId=…, all zero, return
if timeIn is null   -> Status=Absent,     all zero, return

lateMinutes   = MAX(0, timeIn  - (shift.TimeIn + shift.GraceMinutes))
earlyMinutes  = MAX(0, shift.TimeOut - timeOut)          // timeOut null -> 0
workedMinutes = timeOut is null ? 0 : (timeOut - timeIn)

status = workedMinutes >= shift.FullDayMinutes ? Present
       : workedMinutes >= shift.HalfDayMinutes ? HalfDay
       : Absent
```
Grace is applied **only** to late, never to worked minutes. `IgnoreLate` does not change
`LateMinutes` — the true figure is stored; payroll skips the deduction. This keeps the Late Comers
report honest.

### 5.2 On payroll run — `PayrollCalculator` (pure, no DB)

```
per employee:
  1. pay        = latest HrEmployeePay where EffectiveFrom <= monthEnd
  2. basic      = pay.Basic, prorated if joined/left mid-month:
                     basic * (payableCalendarDays / monthDays)
  3. earnings   = fixed components from ComponentsJson
                + rule-driven earnings (overtime)
  4. gross      = basic + earnings
  5. deductions = for each Deduction component, in Priority order:
                     amount = evaluate(component, attendanceSummary, basic, gross)
                     if component.CapPerMonth -> amount = MIN(amount, CapPerMonth)
  6. net        = gross - totalDeductions        // floored at 0, never negative
  7. breakdown  = every line, including zero-value ones where ShowOnPayslip = 1
```

**Edge cases that must be tested:**
- Employee joins on the 15th → basic prorated, only days from the 15th counted
- Employee has no `HrEmployeePay` row → skipped, listed in a warnings panel, run continues
- Late deduction exceeds net pay → net floored at 0, shortfall shown on the payslip
- `IgnoreLate = 1` on a day → minutes recorded, deduction skipped for that day
- Month with no attendance rows at all → all Absent, flagged loudly before locking
- Re-running a Draft run → deletes and regenerates payslips for that run; **blocked if Locked**

### 5.3 Order of operations
Deductions apply **after** gross is final, in `Priority` order, so a percent-of-gross deduction is never
affected by another deduction. Document this; it is the usual source of disputes.

---

## 6. API surface

```
GET    /api/hr/shifts                 CRUD
GET    /api/hr/holidays               CRUD  + POST /copy-year
GET    /api/hr/leave-types            list only (seeded)
GET    /api/hr/pay-components         CRUD
POST   /api/hr/pay-components/preview { componentId, basic, lateMinutes, … } -> amount

GET    /api/hr/attendance/month       ?year=&month=&designationId=     -> grid
POST   /api/hr/attendance/bulk        save the edited grid (upsert)
POST   /api/hr/attendance/fill        bulk mark present/absent for a range

GET    /api/hr/leaves                 ?status=
POST   /api/hr/leaves                 apply
PUT    /api/hr/leaves/{id}/status     approve / reject

GET    /api/hr/employee-pay/{empId}   structure history
POST   /api/hr/employee-pay           new effective-dated row

GET    /api/hr/payroll/runs
POST   /api/hr/payroll/runs/generate  { year, month }  -> creates/regenerates Draft
GET    /api/hr/payroll/runs/{id}      payslip grid
PUT    /api/hr/payroll/runs/{id}/lock
PUT    /api/hr/payroll/runs/{id}/pay  { paidDate, method, reference }
GET    /api/hr/payroll/payslip/{id}/pdf
POST   /api/hr/payroll/payslip/{id}/whatsapp

GET    /api/hr/reports/attendance     ?type=daily|monthly|late|absent
GET    /api/hr/reports/salary-sheet   ?runId=   (+ ?format=xlsx)
```

---

## 7. Screens (10)

| # | Screen | Route | Notes |
|---|---|---|---|
| 1 | Payroll Setup | `/hr/setup` | 4 tabs: Shifts · Holidays · Leave Types · Pay Components (with live preview) |
| 2 | Employee | `/Employee` | Rebuild + Payroll tab (shift, card no, off days, teacher link, salary structure) |
| 3 | Attendance | `/hr/attendance` | Month grid, employee × day; cell editor for time/status; bulk fill; `IgnoreLate` toggle |
| 4 | Leave | `/hr/leave` | One list, apply + approve via status actions |
| 5 | Payroll Run | `/hr/payroll` | Generate → review → adjust → lock → mark paid |
| 6 | Payslip | `/hr/payslip` | PDF print + WhatsApp send |
| 7 | Salary Sheet | `/hr/salary-sheet` | Grid + Excel export (bank advice = an export option) |
| 8 | Attendance Report | `/hr/reports/attendance` | Type dropdown: daily / monthly / late / absent |
| 9 | Loans & Advances | `/hr/loans` | Phase 4 only |
| 10 | Attendance Import | `/hr/attendance/import` | Phase 4 only |

Sidebar: uncomment and expand the **HR Payroll** section in
`School.API/Controllers/SidebarController.cs` (~line 152); register the pages in `Menus` +
`RolePermissions` so RBAC applies.

---

## 8. File map

Per the 8-step recipe in `CLAUDE.md`. Use `PaymentMethod` as the CRUD template — it is the cleanest
recent example in the repo.

```
School.API/
  Data/DBModels/HR/           HrShift, HrHoliday, HrLeaveType, HrAttendance, HrLeave,
                              HrPayComponent, HrEmployeePay, HrPayrollRun, HrPayslip
  Data/SchoolDbContext.cs     + 9 DbSets, + OwnsMany(...).ToJson() config
  DTOs/HR/                    per feature
  Repositories/{Interfaces,Implementations}/HR/
  Services/{Interfaces,Implementations}/HR/
  Services/Implementations/HR/AttendanceCalculator.cs      <- pure, tested
  Services/Implementations/HR/PayrollCalculator.cs         <- pure, tested
  Services/Implementations/Attendance/AttendanceImportService.cs   <- Phase 4, shared
  Controllers/HR/
  Scripts/HrPayroll.sql
  Scripts/SpRPT_HrAttendance.sql, SpRPT_HrSalarySheet.sql
  Program.cs                  + AddScoped registrations, compression, EF retry

School.Web/
  Controllers/Hr*Controller.cs
  Views/Hr*/
  wwwroot/js/hr-grid.js       <- one shared editable-grid component for screens 3 and 5

School.Tests/                 <- NEW project (xUnit)
  PayrollCalculatorTests.cs
  AttendanceCalculatorTests.cs
```

**Delete in Phase 1:** `Controllers/EmployeeController.cs`, `Controllers/SalaryController.cs`,
`Services/{Interfaces,Implementations}/HR/{Employee,Salary}Service.cs`,
`Repositories/{Interfaces,Implementations}/HR/{Employee,Salary}Repository.cs`,
`Data/DBModels/HR/Salary.cs`. Keep `Designation`, `HRGrade`, `Teacher`.

---

## 9. Performance requirements

**Payroll run — exactly 4 queries, one transaction, target < 2s for 200 employees:**
```
1. employees + active pay structures    (AsNoTracking, projected)
2. attendance aggregate for the month   (GROUP BY EmployeeId)
3. approved leave aggregate             (GROUP BY EmployeeId)
4. due loan installments                (Phase 4)
   -> PayrollCalculator in memory
   -> AddRange + one SaveChanges inside one transaction
```
The failure mode to avoid is a `foreach (employee)` loop that queries per person — 4 queries becomes 800.

**Required:**
- `AsNoTracking()` on every read. The Academic and Fee repositories already do this consistently; the HR
  ones do not — rebuild them to match.
- `IMemoryCache` for shifts, holidays and pay components (they change monthly at most).
  `AddMemoryCache()` is **already registered** at `Program.cs:44` and currently unused for this.
- Attendance month grid: one projected query, no `Include()`. 200 × 31 cells sends fine in one payload.
- **Add response compression** to `Program.cs` — not configured today. The attendance grid and salary
  sheet are the largest JSON payloads in the app and compress ~80%.
- **Add `EnableRetryOnFailure()`** to the SQL Server connection — not configured today.
- Reports as raw SQL only; never materialise a report through EF entities.

---

## 10. Test plan

New `School.Tests` (xUnit) — the repo currently has **no test project**. Scope is deliberately narrow:
the two pure calculators, where the money is computed. Everything else is CRUD and covered by use.

| Suite | Cases |
|---|---|
| `AttendanceCalculatorTests` | on time; within grace; past grace; half day; absent (no in); no out; holiday; weekly off; approved leave; `IgnoreLate` records minutes |
| `PayrollCalculatorTests` | Sir Syed per-minute rule end-to-end; grace applied; `CapPerDay`; `CapPerMonth`; `FreeUnitsPerMonth`; slab; fixed rate; percent-of-basic; mid-month joiner proration; missing pay structure; net floored at 0; deduction ordering |

---

## 11. Phases — 12–16 days

### Phase 1 — Foundation + Attendance (5–6 days) — *ships usable on its own*
- [ ] Delete the dead HR scaffolding (section 8)
- [ ] `Scripts/HrPayroll.sql` — all 9 tables, indexes, `Employees` columns
- [ ] Models + DbSets + JSON config
- [ ] Employee rebuild: working CRUD + 5 new columns + teacher link dropdown
- [ ] Screen 1 (Setup: Shifts + Holidays tabs), Screen 2 (Employee), Screen 3 (Attendance grid)
- [ ] `AttendanceCalculator` + `School.Tests` + its test suite
- [ ] Screen 8 (Attendance Report) + `SpRPT_HrAttendance.sql`
- [ ] Sidebar + RBAC registration

**Ships:** staff attendance end-to-end. No payroll yet.

### Phase 2 — Payroll engine (4–5 days)
- [ ] `HrPayComponent`, `HrEmployeePay`, `HrPayrollRun`, `HrPayslip` stacks
- [ ] Setup screen: Pay Components tab + live preview endpoint
- [ ] `PayrollCalculator` + full test suite (section 10)
- [ ] Screen 5 (Payroll Run), Screen 6 (Payslip PDF via `PuppeteerPdfRenderer`), Screen 7 (Salary Sheet + Excel — add `ClosedXML`)
- [ ] The 4-query run; response compression; EF retry

**Ships:** full monthly payroll on Sir Syed's per-minute rule.

### Phase 3 — Leave (2 days)
- [ ] `HrLeave` + Screen 4; approved leave feeds attendance status

### Phase 4 — Optional, only if asked (3–4 days)
- [ ] Loans & advances (`HrLoan`, `HrLoanInstallment`, Screen 9)
- [ ] Hikvision import (Screen 10): shared `AttendanceImportService`, aggregate first-in/last-out at
      import, two writers — staff → `HrAttendance`, students → existing `StdAttendence`
- [ ] Payslip over WhatsApp via the existing WAHA sender + `SmsTemplateCatalog`

---

## 12. Defaults in use

Nothing blocks Phase 1. These ship as defaults and are changeable from the Setup screen without a
migration — confirm with the client during Phase 1 and adjust:

| Setting | Default |
|---|---|
| Grace period | 10 minutes |
| Late deduction cap | Rs 500 / day, no monthly cap |
| Month days basis | Fixed 30 |
| Weekly off | Sunday only |
| Working hours | From the shift |
| Summer/winter timings | Single shift; `HrShift.EffectiveFrom/To` already supports a second one |
| Free lates per month | 0 |

Student attendance from the same Hikvision machine is Phase 4 and needs no schema change now — the import
service routes by card mapping, staff to `HrAttendance` and students to the existing `StdAttendence`.

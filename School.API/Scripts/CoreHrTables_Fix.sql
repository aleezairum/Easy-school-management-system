-- ============================================================================
-- One-time corrective script: CreateTables.sql predates the current Employee
-- model and never included Designations/HRGrades at all. Run this AFTER
-- CreateTables.sql and BEFORE HrPayroll.sql.
--
-- Safe to re-run (idempotent). Does not touch the legacy Employees.Designation
-- (free-text) column if present — it's simply unused by the current EF model.
-- ============================================================================

------------------------------------------------------------------ Designations
IF OBJECT_ID('Designations','U') IS NULL
CREATE TABLE Designations (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Code        NVARCHAR(20)  NOT NULL,
    Name        NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255) NULL,
    SortOrder   INT           NULL,
    IsActive    BIT           NOT NULL CONSTRAINT DF_Desig_Act DEFAULT 1,
    CreatedAt   DATETIME2(0)  NOT NULL CONSTRAINT DF_Desig_CA DEFAULT SYSUTCDATETIME(),
    UpdatedAt   DATETIME2(0)  NULL
);
GO

------------------------------------------------------------------ HRGrades
IF OBJECT_ID('HRGrades','U') IS NULL
CREATE TABLE HRGrades (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Code        NVARCHAR(50)  NOT NULL,
    Name        NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255) NULL,
    MinSalary   DECIMAL(18,2) NULL,
    MaxSalary   DECIMAL(18,2) NULL,
    SortOrder   INT           NULL,
    IsActive    BIT           NOT NULL CONSTRAINT DF_HRGrade_Act DEFAULT 1,
    CreatedAt   DATETIME2(0)  NOT NULL CONSTRAINT DF_HRGrade_CA DEFAULT SYSUTCDATETIME(),
    UpdatedAt   DATETIME2(0)  NULL
);
GO

------------------------------------------------------------------ Employees: bring the CreateTables.sql-era
------------------------------------------------------------------ table up to the current Employee.cs shape.
IF COL_LENGTH('Employees','FirstNameUrdu')          IS NULL ALTER TABLE Employees ADD FirstNameUrdu NVARCHAR(100) NULL;
IF COL_LENGTH('Employees','LastNameUrdu')           IS NULL ALTER TABLE Employees ADD LastNameUrdu NVARCHAR(100) NULL;
IF COL_LENGTH('Employees','CNIC')                   IS NULL ALTER TABLE Employees ADD CNIC NVARCHAR(20) NULL;
IF COL_LENGTH('Employees','Mobile')                 IS NULL ALTER TABLE Employees ADD Mobile NVARCHAR(20) NULL;
IF COL_LENGTH('Employees','Gender')                 IS NULL ALTER TABLE Employees ADD Gender NVARCHAR(10) NULL;
IF COL_LENGTH('Employees','DateOfBirth')            IS NULL ALTER TABLE Employees ADD DateOfBirth DATETIME2(0) NULL;
IF COL_LENGTH('Employees','JoiningDate')            IS NULL ALTER TABLE Employees ADD JoiningDate DATETIME2(0) NULL;
IF COL_LENGTH('Employees','LeavingDate')            IS NULL ALTER TABLE Employees ADD LeavingDate DATETIME2(0) NULL;
IF COL_LENGTH('Employees','Address')                IS NULL ALTER TABLE Employees ADD Address NVARCHAR(500) NULL;
IF COL_LENGTH('Employees','City')                   IS NULL ALTER TABLE Employees ADD City NVARCHAR(100) NULL;
IF COL_LENGTH('Employees','Photo')                  IS NULL ALTER TABLE Employees ADD Photo NVARCHAR(255) NULL;
IF COL_LENGTH('Employees','Qualification')          IS NULL ALTER TABLE Employees ADD Qualification NVARCHAR(100) NULL;
IF COL_LENGTH('Employees','Specialization')         IS NULL ALTER TABLE Employees ADD Specialization NVARCHAR(100) NULL;
IF COL_LENGTH('Employees','Experience')             IS NULL ALTER TABLE Employees ADD Experience NVARCHAR(100) NULL;
IF COL_LENGTH('Employees','DesignationId')          IS NULL ALTER TABLE Employees ADD DesignationId INT NULL;
IF COL_LENGTH('Employees','HRGradeId')              IS NULL ALTER TABLE Employees ADD HRGradeId INT NULL;
IF COL_LENGTH('Employees','AcademicGradeId')        IS NULL ALTER TABLE Employees ADD AcademicGradeId INT NULL;
IF COL_LENGTH('Employees','BankName')               IS NULL ALTER TABLE Employees ADD BankName NVARCHAR(100) NULL;
IF COL_LENGTH('Employees','BankAccountNo')          IS NULL ALTER TABLE Employees ADD BankAccountNo NVARCHAR(50) NULL;
IF COL_LENGTH('Employees','EmergencyContactName')   IS NULL ALTER TABLE Employees ADD EmergencyContactName NVARCHAR(100) NULL;
IF COL_LENGTH('Employees','EmergencyContactPhone')  IS NULL ALTER TABLE Employees ADD EmergencyContactPhone NVARCHAR(20) NULL;
IF COL_LENGTH('Employees','EmergencyContactRelation') IS NULL ALTER TABLE Employees ADD EmergencyContactRelation NVARCHAR(50) NULL;
IF COL_LENGTH('Employees','EmployeeType')           IS NULL ALTER TABLE Employees ADD EmployeeType NVARCHAR(50) NULL;
IF COL_LENGTH('Employees','Status')                 IS NULL ALTER TABLE Employees ADD Status NVARCHAR(50) NULL CONSTRAINT DF_Emp_Status DEFAULT 'Active';
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Employees_Designations')
    ALTER TABLE Employees ADD CONSTRAINT FK_Employees_Designations FOREIGN KEY (DesignationId) REFERENCES Designations(Id);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Employees_HRGrades')
    ALTER TABLE Employees ADD CONSTRAINT FK_Employees_HRGrades FOREIGN KEY (HRGradeId) REFERENCES HRGrades(Id);
GO

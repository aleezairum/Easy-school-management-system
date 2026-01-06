-- =============================================
-- School Management System - Database Tables
-- =============================================

-- Create Database (if not exists)
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'SchoolManagementDB')
BEGIN
    CREATE DATABASE SchoolManagementDB;
END
GO

USE SchoolManagementDB;
GO

-- =============================================
-- Table: Menus
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Menus]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Menus] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [MenuTitle] NVARCHAR(100) NOT NULL,
        [Url] NVARCHAR(200) NULL,
        [Icon] NVARCHAR(50) NULL,
        [ParentId] INT NULL,
        [DisplayOrder] INT NOT NULL DEFAULT 0,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [Level] INT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [FK_Menus_Parent] FOREIGN KEY ([ParentId]) REFERENCES [dbo].[Menus]([Id])
    );

    CREATE INDEX [IX_Menus_MenuTitle] ON [dbo].[Menus]([MenuTitle]);
    CREATE INDEX [IX_Menus_ParentId] ON [dbo].[Menus]([ParentId]);
END
GO

-- =============================================
-- Table: Roles
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Roles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Roles] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [RoleName] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [UQ_Roles_RoleName] UNIQUE ([RoleName])
    );
END
GO

-- =============================================
-- Table: RolePermissions
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RolePermissions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RolePermissions] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [RoleId] INT NOT NULL,
        [MenuId] INT NOT NULL,
        [IsView] BIT NOT NULL DEFAULT 0,
        [IsInsert] BIT NOT NULL DEFAULT 0,
        [IsUpdate] BIT NOT NULL DEFAULT 0,
        [IsDelete] BIT NOT NULL DEFAULT 0,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [IsBackDate] BIT NOT NULL DEFAULT 0,
        [IsPrint] BIT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [FK_RolePermissions_Role] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RolePermissions_Menu] FOREIGN KEY ([MenuId]) REFERENCES [dbo].[Menus]([Id]) ON DELETE CASCADE,
        CONSTRAINT [UQ_RolePermissions_RoleMenu] UNIQUE ([RoleId], [MenuId])
    );
END
GO

-- =============================================
-- Table: Employees
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Employees]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Employees] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [EmployeeCode] NVARCHAR(50) NOT NULL,
        [FirstName] NVARCHAR(100) NOT NULL,
        [LastName] NVARCHAR(100) NULL,
        [Email] NVARCHAR(200) NULL,
        [Phone] NVARCHAR(20) NULL,
        [Department] NVARCHAR(100) NULL,
        [Designation] NVARCHAR(100) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [UQ_Employees_EmployeeCode] UNIQUE ([EmployeeCode])
    );
END
GO

-- =============================================
-- Table: Users
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Users] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [EmployeeId] INT NULL,
        [UserFullName] NVARCHAR(200) NOT NULL,
        [UserLogin] NVARCHAR(100) NOT NULL,
        [PasswordHash] NVARCHAR(500) NOT NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL,
        [LastLoginAt] DATETIME2 NULL,
        CONSTRAINT [FK_Users_Employee] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees]([Id]) ON DELETE SET NULL,
        CONSTRAINT [UQ_Users_UserLogin] UNIQUE ([UserLogin])
    );
END
GO

-- =============================================
-- Table: UserRoles
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserRoles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[UserRoles] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] INT NOT NULL,
        [RoleId] INT NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [FK_UserRoles_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRoles_Role] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles]([Id]) ON DELETE CASCADE,
        CONSTRAINT [UQ_UserRoles_UserRole] UNIQUE ([UserId], [RoleId])
    );
END
GO

-- =============================================
-- Table: Sessions
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Sessions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Sessions] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [SessionName] NVARCHAR(50) NOT NULL,
        [StartDate] DATETIME2 NOT NULL,
        [EndDate] DATETIME2 NOT NULL,
        [IsCurrent] BIT NOT NULL DEFAULT 0,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [UQ_Sessions_SessionName] UNIQUE ([SessionName])
    );
END
GO

-- =============================================
-- Table: SMSClass
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SMSClass]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SMSClass] (
        [VID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [VName] NVARCHAR(100) NOT NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [InsertedBy] INT NULL,
        [InsertedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [InsertedIp] NVARCHAR(50) NULL,
        [UpdatedBy] INT NULL,
        [UpdatedDate] DATETIME2 NULL,
        [UpdatedIp] NVARCHAR(50) NULL,
        CONSTRAINT [UQ_SMSClass_VName] UNIQUE ([VName])
    );
END
GO

-- =============================================
-- Table: SMSSection
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SMSSection]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SMSSection] (
        [VID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [VName] NVARCHAR(100) NOT NULL,
        [ClassID] INT NOT NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [InsertedBy] INT NULL,
        [InsertedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [InsertedIp] NVARCHAR(50) NULL,
        [UpdatedBy] INT NULL,
        [UpdatedDate] DATETIME2 NULL,
        [UpdatedIp] NVARCHAR(50) NULL,
        CONSTRAINT [FK_SMSSection_Class] FOREIGN KEY ([ClassID]) REFERENCES [dbo].[SMSClass]([VID]) ON DELETE CASCADE,
        CONSTRAINT [UQ_SMSSection_ClassSection] UNIQUE ([ClassID], [VName])
    );

    CREATE INDEX [IX_SMSSection_ClassID] ON [dbo].[SMSSection]([ClassID]);
END
GO

-- =============================================
-- Table: SMSSubject
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SMSSubject]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SMSSubject] (
        [VID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [VName] NVARCHAR(100) NOT NULL,
        [ClassID] INT NOT NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [InsertedBy] INT NULL,
        [InsertedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [InsertedIp] NVARCHAR(50) NULL,
        [UpdatedBy] INT NULL,
        [UpdatedDate] DATETIME2 NULL,
        [UpdatedIp] NVARCHAR(50) NULL,
        CONSTRAINT [FK_SMSSubject_Class] FOREIGN KEY ([ClassID]) REFERENCES [dbo].[SMSClass]([VID]) ON DELETE CASCADE,
        CONSTRAINT [UQ_SMSSubject_ClassSubject] UNIQUE ([ClassID], [VName])
    );

    CREATE INDEX [IX_SMSSubject_ClassID] ON [dbo].[SMSSubject]([ClassID]);
END
GO

-- =============================================
-- Table: Institutes
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Institutes]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Institutes] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(200) NOT NULL,
        [Code] NVARCHAR(50) NOT NULL,
        [Address] NVARCHAR(500) NULL,
        [Phone] NVARCHAR(20) NULL,
        [Email] NVARCHAR(200) NULL,
        [Website] NVARCHAR(200) NULL,
        [LogoUrl] NVARCHAR(500) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [UQ_Institutes_Code] UNIQUE ([Code])
    );
END
GO

-- =============================================
-- Table: Admissions
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Admissions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Admissions] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,

        -- Admission Info
        [DateOfAdmission] DATETIME2 NULL,
        [AdmissionNo] NVARCHAR(50) NULL,
        [ClassSought] NVARCHAR(100) NULL,

        -- Student Info
        [NameOfStudent] NVARCHAR(200) NOT NULL,
        [NameOfStudentUrdu] NVARCHAR(200) NULL,

        -- Father Info
        [FatherName] NVARCHAR(200) NULL,
        [FatherNameUrdu] NVARCHAR(200) NULL,
        [FatherCNIC] NVARCHAR(20) NULL,
        [FatherOccupation] NVARCHAR(100) NULL,
        [FatherMobile] NVARCHAR(20) NULL,

        -- Mother Info
        [MotherName] NVARCHAR(200) NULL,
        [MotherCNIC] NVARCHAR(20) NULL,
        [MotherMobile] NVARCHAR(20) NULL,

        -- Guardian Info
        [GuardianName] NVARCHAR(200) NULL,
        [GuardianCNIC] NVARCHAR(20) NULL,
        [GuardianRelation] NVARCHAR(100) NULL,
        [GuardianMobile] NVARCHAR(20) NULL,

        -- Student Details
        [DateOfBirth] DATETIME2 NULL,
        [DateOfBirthInWords] NVARCHAR(200) NULL,
        [PlaceOfBirth] NVARCHAR(200) NULL,
        [FormBNo] NVARCHAR(50) NULL,
        [Gender] NVARCHAR(20) NULL,
        [Religion] NVARCHAR(50) NULL,

        -- Address
        [PresentAddress] NVARCHAR(500) NULL,
        [PresentAddressUrdu] NVARCHAR(500) NULL,
        [PermanentAddress] NVARCHAR(500) NULL,
        [PermanentAddressUrdu] NVARCHAR(500) NULL,
        [PhoneResidence] NVARCHAR(20) NULL,
        [EmergencyContact] NVARCHAR(20) NULL,

        -- Previous School
        [PreviousSchool] NVARCHAR(300) NULL,
        [LastClass] NVARCHAR(50) NULL,
        [Board] NVARCHAR(100) NULL,
        [YearOfPassing] NVARCHAR(10) NULL,
        [MarksObtained] NVARCHAR(10) NULL,
        [TotalMarks] NVARCHAR(10) NULL,
        [Percentage] NVARCHAR(10) NULL,

        -- For Office Use Only
        [RegistrationNo] NVARCHAR(50) NULL,
        [RollNo] NVARCHAR(50) NULL,
        [AdmissionFee] DECIMAL(18,2) NULL,
        [TuitionFee] DECIMAL(18,2) NULL,
        [OtherCharges] DECIMAL(18,2) NULL,
        [TotalFee] DECIMAL(18,2) NULL,
        [TestMarks] NVARCHAR(10) NULL,
        [TestTotalMarks] NVARCHAR(10) NULL,
        [TestPercentage] NVARCHAR(10) NULL,
        [TestGrade] NVARCHAR(10) NULL,
        [Remarks] NVARCHAR(500) NULL,

        -- Status
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL
    );

    CREATE INDEX [IX_Admissions_AdmissionNo] ON [dbo].[Admissions]([AdmissionNo]);
    CREATE INDEX [IX_Admissions_NameOfStudent] ON [dbo].[Admissions]([NameOfStudent]);
    CREATE INDEX [IX_Admissions_Status] ON [dbo].[Admissions]([Status]);
END
GO

-- =============================================
-- Seed Data: Menus
-- =============================================
SET IDENTITY_INSERT [dbo].[Menus] ON;

-- Level 0 - Main Categories
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 1, 'Dashboard', '/Dashboard', 'bi-grid-1x2-fill', NULL, 1, 0, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 1);
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 2, 'Administration', NULL, 'bi-gear-fill', NULL, 2, 0, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 2);
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 3, 'Student Management', NULL, 'bi-people-fill', NULL, 3, 0, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 3);
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 4, 'Teacher Management', NULL, 'bi-person-badge-fill', NULL, 4, 0, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 4);
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 5, 'Academic', NULL, 'bi-book-fill', NULL, 5, 0, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 5);
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 6, 'Finance', NULL, 'bi-currency-dollar', NULL, 6, 0, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 6);
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 7, 'Reports', NULL, 'bi-file-earmark-bar-graph-fill', NULL, 7, 0, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 7);
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 8, 'Settings', NULL, 'bi-sliders', NULL, 8, 0, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 8);

-- Level 1 - Administration Submenu
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 9, '----> User Management', '/Users', 'bi-person-fill', 2, 1, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 9);
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 10, '----> Role Permissions', '/Roles', 'bi-shield-lock-fill', 2, 2, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 10);
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 11, '----> Institute Setup', '/Institute', 'bi-building', 2, 3, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 11);
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 12, '----> Session Setup', '/Sessions', 'bi-calendar-range', 2, 4, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 12);

-- Level 1 - Student Management Submenu
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 13, '----> Student Registration', '/Students/Registration', 'bi-person-plus-fill', 3, 1, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 13);
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 14, '----> Student List', '/Students', 'bi-list-ul', 3, 2, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 14);
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 15, '----> Student Attendance', '/Students/Attendance', 'bi-calendar-check-fill', 3, 3, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 15);
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 16, '----> Student Promotion', '/Students/Promotion', 'bi-arrow-up-circle-fill', 3, 4, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 16);

-- Level 1 - Teacher Management Submenu
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 17, '----> Teacher Registration', '/Teachers/Registration', 'bi-person-plus-fill', 4, 1, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 17);
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 18, '----> Teacher List', '/Teachers', 'bi-list-ul', 4, 2, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 18);
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 19, '----> Teacher Attendance', '/Teachers/Attendance', 'bi-calendar-check-fill', 4, 3, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 19);

-- Level 1 - Academic Submenu
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 20, '----> Class Setup', '/Classes', 'bi-door-open-fill', 5, 1, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 20);
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 21, '----> Subject Setup', '/Subjects', 'bi-journal-bookmark-fill', 5, 2, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 21);
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 22, '----> Timetable', '/Timetable', 'bi-calendar-event-fill', 5, 3, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 22);
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 23, '----> Examinations', '/Examinations', 'bi-clipboard2-data-fill', 5, 4, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 23);
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 24, '----> Results', '/Results', 'bi-award-fill', 5, 5, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 24);

-- Level 1 - Finance Submenu
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 25, '----> Fee Structure', '/Fees/Structure', 'bi-cash-stack', 6, 1, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 25);
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 26, '----> Fee Collection', '/Fees/Collection', 'bi-receipt', 6, 2, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 26);
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 27, '----> Fee Reports', '/Fees/Reports', 'bi-file-earmark-text-fill', 6, 3, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 27);
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 28, '----> Salary Management', '/Salary', 'bi-wallet2', 6, 4, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 28);

-- Level 1 - Reports Submenu
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 29, '----> Student Reports', '/Reports/Students', 'bi-file-person-fill', 7, 1, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 29);
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 30, '----> Teacher Reports', '/Reports/Teachers', 'bi-file-person-fill', 7, 2, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 30);
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 31, '----> Attendance Reports', '/Reports/Attendance', 'bi-calendar-check-fill', 7, 3, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 31);
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 32, '----> Financial Reports', '/Reports/Financial', 'bi-graph-up-arrow', 7, 4, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 32);

-- Level 1 - Settings Submenu
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 33, '----> General Settings', '/Settings/General', 'bi-gear-fill', 8, 1, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 33);
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 34, '----> Email Settings', '/Settings/Email', 'bi-envelope-fill', 8, 2, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 34);
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 35, '----> SMS Settings', '/Settings/SMS', 'bi-chat-dots-fill', 8, 3, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 35);
INSERT INTO [dbo].[Menus] ([Id], [MenuTitle], [Url], [Icon], [ParentId], [DisplayOrder], [Level], [IsActive])
SELECT 36, '----> Backup & Restore', '/Settings/Backup', 'bi-cloud-arrow-up-fill', 8, 4, 1, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Id] = 36);

SET IDENTITY_INSERT [dbo].[Menus] OFF;
GO

-- =============================================
-- Seed Data: Roles
-- =============================================
SET IDENTITY_INSERT [dbo].[Roles] ON;

INSERT INTO [dbo].[Roles] ([Id], [RoleName], [Description], [IsActive])
SELECT 1, 'Super Admin', 'Full access to all modules and features', 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = 1);
INSERT INTO [dbo].[Roles] ([Id], [RoleName], [Description], [IsActive])
SELECT 2, 'Admin', 'Administrative access with limited settings', 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = 2);
INSERT INTO [dbo].[Roles] ([Id], [RoleName], [Description], [IsActive])
SELECT 3, 'Principal', 'Access to academic and administrative reports', 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = 3);
INSERT INTO [dbo].[Roles] ([Id], [RoleName], [Description], [IsActive])
SELECT 4, 'Teacher', 'Access to student management and attendance', 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = 4);
INSERT INTO [dbo].[Roles] ([Id], [RoleName], [Description], [IsActive])
SELECT 5, 'Accountant', 'Access to finance module', 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = 5);
INSERT INTO [dbo].[Roles] ([Id], [RoleName], [Description], [IsActive])
SELECT 6, 'Receptionist', 'Limited access to student and visitor management', 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = 6);

SET IDENTITY_INSERT [dbo].[Roles] OFF;
GO

-- =============================================
-- Seed Data: Employees
-- =============================================
SET IDENTITY_INSERT [dbo].[Employees] ON;

INSERT INTO [dbo].[Employees] ([Id], [EmployeeCode], [FirstName], [LastName], [Email], [Phone], [Department], [Designation], [IsActive])
SELECT 1, 'EMP001', 'John', 'Doe', 'john.doe@school.com', '123-456-7890', 'Administration', 'Administrator', 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Employees] WHERE [Id] = 1);
INSERT INTO [dbo].[Employees] ([Id], [EmployeeCode], [FirstName], [LastName], [Email], [Phone], [Department], [Designation], [IsActive])
SELECT 2, 'EMP002', 'Jane', 'Smith', 'jane.smith@school.com', '123-456-7891', 'Academic', 'Principal', 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Employees] WHERE [Id] = 2);
INSERT INTO [dbo].[Employees] ([Id], [EmployeeCode], [FirstName], [LastName], [Email], [Phone], [Department], [Designation], [IsActive])
SELECT 3, 'EMP003', 'Robert', 'Johnson', 'robert.j@school.com', '123-456-7892', 'Academic', 'Vice Principal', 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Employees] WHERE [Id] = 3);
INSERT INTO [dbo].[Employees] ([Id], [EmployeeCode], [FirstName], [LastName], [Email], [Phone], [Department], [Designation], [IsActive])
SELECT 4, 'EMP004', 'Emily', 'Brown', 'emily.b@school.com', '123-456-7893', 'Finance', 'Accountant', 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Employees] WHERE [Id] = 4);
INSERT INTO [dbo].[Employees] ([Id], [EmployeeCode], [FirstName], [LastName], [Email], [Phone], [Department], [Designation], [IsActive])
SELECT 5, 'EMP005', 'Michael', 'Wilson', 'michael.w@school.com', '123-456-7894', 'IT', 'IT Manager', 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Employees] WHERE [Id] = 5);
INSERT INTO [dbo].[Employees] ([Id], [EmployeeCode], [FirstName], [LastName], [Email], [Phone], [Department], [Designation], [IsActive])
SELECT 6, 'EMP006', 'Sarah', 'Davis', 'sarah.d@school.com', '123-456-7895', 'Academic', 'Teacher', 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Employees] WHERE [Id] = 6);
INSERT INTO [dbo].[Employees] ([Id], [EmployeeCode], [FirstName], [LastName], [Email], [Phone], [Department], [Designation], [IsActive])
SELECT 7, 'EMP007', 'David', 'Miller', 'david.m@school.com', '123-456-7896', 'Academic', 'Teacher', 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Employees] WHERE [Id] = 7);
INSERT INTO [dbo].[Employees] ([Id], [EmployeeCode], [FirstName], [LastName], [Email], [Phone], [Department], [Designation], [IsActive])
SELECT 8, 'EMP008', 'Lisa', 'Taylor', 'lisa.t@school.com', '123-456-7897', 'HR', 'HR Manager', 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Employees] WHERE [Id] = 8);
INSERT INTO [dbo].[Employees] ([Id], [EmployeeCode], [FirstName], [LastName], [Email], [Phone], [Department], [Designation], [IsActive])
SELECT 9, 'EMP009', 'James', 'Anderson', 'james.a@school.com', '123-456-7898', 'Administration', 'Office Assistant', 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Employees] WHERE [Id] = 9);
INSERT INTO [dbo].[Employees] ([Id], [EmployeeCode], [FirstName], [LastName], [Email], [Phone], [Department], [Designation], [IsActive])
SELECT 10, 'EMP010', 'Jennifer', 'Thomas', 'jennifer.t@school.com', '123-456-7899', 'Library', 'Librarian', 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Employees] WHERE [Id] = 10);

SET IDENTITY_INSERT [dbo].[Employees] OFF;
GO

-- =============================================
-- Seed Data: Sessions
-- =============================================
SET IDENTITY_INSERT [dbo].[Sessions] ON;

INSERT INTO [dbo].[Sessions] ([Id], [SessionName], [StartDate], [EndDate], [IsCurrent], [IsActive])
SELECT 1, '2023-2024', '2023-04-01', '2024-03-31', 0, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Sessions] WHERE [Id] = 1);
INSERT INTO [dbo].[Sessions] ([Id], [SessionName], [StartDate], [EndDate], [IsCurrent], [IsActive])
SELECT 2, '2024-2025', '2024-04-01', '2025-03-31', 1, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Sessions] WHERE [Id] = 2);
INSERT INTO [dbo].[Sessions] ([Id], [SessionName], [StartDate], [EndDate], [IsCurrent], [IsActive])
SELECT 3, '2025-2026', '2025-04-01', '2026-03-31', 0, 1 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Sessions] WHERE [Id] = 3);

SET IDENTITY_INSERT [dbo].[Sessions] OFF;
GO

-- =============================================
-- Seed Data: Default Admin User
-- =============================================
-- Password: admin123 (SHA256 hash)
INSERT INTO [dbo].[Users] ([EmployeeId], [UserFullName], [UserLogin], [PasswordHash], [IsActive])
SELECT 1, 'Admin User', 'admin@school.com', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', 1
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [UserLogin] = 'admin@school.com');
GO

-- Assign Super Admin role to default admin user
INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId])
SELECT u.Id, 1 FROM [dbo].[Users] u
WHERE u.[UserLogin] = 'admin@school.com'
AND NOT EXISTS (SELECT 1 FROM [dbo].[UserRoles] WHERE [UserId] = u.Id AND [RoleId] = 1);
GO

PRINT 'Database tables and seed data created successfully!';
GO

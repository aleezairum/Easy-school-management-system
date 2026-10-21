/* ============================================================================
   EasySchool mobile app (Android / iPhone): logins, feature rights, devices.
   - AppAccount: one login per student (a parent signs in with any child's
     login and sees the whole family) and per member of staff. Passwords are
     PBKDF2 hashes; InitialPassword is kept only so the office can reprint the
     login slip, and is cleared once the user sets their own.
   - AppRoleFeature: what each role may do in the app (User Management ->
     Mobile App Permissions). The Admin role may do everything.
   - AppDevice: phones signed in, for push notifications.
   - AppPushLog: which announcements have been pushed to the app.
   "Admin" and "Teacher" roles are added when missing (Teacher may see its classes).
   Safe to run again.
   ============================================================================ */
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.AppAccount', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppAccount
    (
        Id                  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppAccount PRIMARY KEY,
        AccountType         TINYINT        NOT NULL,           -- 1 student, 2 staff
        StudentId           INT            NULL,
        EmployeeId          INT            NULL,
        LoginId             NVARCHAR(50)   NOT NULL,
        DisplayName         NVARCHAR(150)  NULL,
        PasswordHash        VARBINARY(64)  NOT NULL,
        PasswordSalt        VARBINARY(32)  NOT NULL,
        InitialPassword     NVARCHAR(20)   NULL,
        MustChangePassword  BIT            NOT NULL CONSTRAINT DF_AppAcc_Must DEFAULT (1),
        RoleId              INT            NULL,               -- staff: Roles.Id
        IsActive            BIT            NOT NULL CONSTRAINT DF_AppAcc_Active DEFAULT (1),
        FailedLogins        INT            NOT NULL CONSTRAINT DF_AppAcc_Failed DEFAULT (0),
        LockedUntil         DATETIME2      NULL,
        LastLoginAt         DATETIME2      NULL,
        CreatedAt           DATETIME2      NOT NULL CONSTRAINT DF_AppAcc_Created DEFAULT (SYSUTCDATETIME()),
        CreatedBy           INT            NULL,
        UpdatedAt           DATETIME2      NULL,
        CONSTRAINT CK_AppAcc_Type CHECK (AccountType IN (1, 2))
    );
    CREATE UNIQUE INDEX UX_AppAcc_Login ON dbo.AppAccount (AccountType, LoginId);
    CREATE UNIQUE INDEX UX_AppAcc_Student ON dbo.AppAccount (StudentId) WHERE StudentId IS NOT NULL;
    CREATE UNIQUE INDEX UX_AppAcc_Employee ON dbo.AppAccount (EmployeeId) WHERE EmployeeId IS NOT NULL;
END
GO

IF OBJECT_ID('dbo.AppRoleFeature', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppRoleFeature
    (
        RoleId      INT           NOT NULL,
        FeatureKey  VARCHAR(50)   NOT NULL,
        CONSTRAINT PK_AppRoleFeature PRIMARY KEY (RoleId, FeatureKey)
    );
END
GO

IF OBJECT_ID('dbo.AppDevice', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppDevice
    (
        Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppDevice PRIMARY KEY,
        AccountId   INT            NOT NULL CONSTRAINT FK_AppDevice_Account REFERENCES dbo.AppAccount(Id) ON DELETE CASCADE,
        Mode        VARCHAR(10)    NOT NULL,                   -- student | parent | staff
        Token       NVARCHAR(400)  NOT NULL,
        Platform    VARCHAR(10)    NULL,                       -- android | ios
        AppVersion  VARCHAR(20)    NULL,
        LastSeenAt  DATETIME2      NOT NULL CONSTRAINT DF_AppDevice_Seen DEFAULT (SYSUTCDATETIME())
    );
    CREATE UNIQUE INDEX UX_AppDevice_Token ON dbo.AppDevice (Token);
END
GO

-- An Admin role (the Principal's app login: everything - school dashboard, any
-- student, notices) and a Teacher role, allowed to see its own classes. Only
-- the mobile app reads these; the web menu does not filter on roles.
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = 'Admin')
    INSERT dbo.Roles (RoleName, Description, IsActive, CreatedAt) VALUES ('Admin', 'Principal / administrator', 1, SYSUTCDATETIME());
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = 'Teacher')
    INSERT dbo.Roles (RoleName, Description, IsActive, CreatedAt) VALUES ('Teacher', 'Teaching staff', 1, SYSUTCDATETIME());
GO
INSERT dbo.AppRoleFeature (RoleId, FeatureKey)
SELECT R.Id, 'app.class.view' FROM dbo.Roles R
WHERE R.RoleName = 'Teacher'
  AND NOT EXISTS (SELECT 1 FROM dbo.AppRoleFeature F WHERE F.RoleId = R.Id);
GO

SELECT 'Mobile app : ' + CASE WHEN OBJECT_ID('dbo.AppDevice') IS NULL THEN 'missing' ELSE 'ready' END;
GO

-- One row per announcement once its push notification has gone (or was
-- skipped), so each announcement is pushed only once.
--   Status: sent | skipped | failed
IF OBJECT_ID('dbo.AppPushLog', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppPushLog
    (
        AnnouncementId  INT            NOT NULL CONSTRAINT PK_AppPushLog PRIMARY KEY,
        Status          VARCHAR(10)    NOT NULL,
        Detail          NVARCHAR(500)  NULL,
        SentAt          DATETIME2      NOT NULL CONSTRAINT DF_AppPushLog_Sent DEFAULT (SYSUTCDATETIME())
    );
END
GO

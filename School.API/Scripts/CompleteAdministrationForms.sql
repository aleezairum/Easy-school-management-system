/* ============================================================================
   Administration module — the tables behind the four forms
   ============================================================================

   Deliberately NO "USE [EasySchoolDB]" here: a hard-coded USE overrides
   sqlcmd's -d and sends the script to whatever that name resolves to on the
   machine it runs on. The database comes from -d instead.

     cd School.API\Scripts
     sqlcmd -S . -U sa -P <pwd> -d EasySchoolDB -i CompleteAdministrationForms.sql

   What this does, and why:

   1. Institutes — the School Profile form asks for 15 things and the table
      held 5 of them. The ten missing columns are added, and LogoUrl is widened
      to NVARCHAR(MAX) so the logo can be stored inline (a data URL) instead of
      depending on a file share that has to survive a redeploy.

   2. Menus — this is the list the Page Permissions form edits and the Roles &
      Permissions form assigns rights against. It held a generic template
      shipped with the starter project ("Teacher Management", "Examinations >
      Schedule", "Email Settings") describing an application that does not
      exist here, so every permission ever granted pointed at a fiction. It is
      rebuilt from the real sidebar: 8 sections and the pages under them.

      Two columns are added first, because the Page Permissions form edits
      them: RoutingPageUrl and SrNo.

   3. Nothing here touches Backup & Restore, and nothing drops a table.
      RolePermissions rows that point at menu ids which no longer exist are
      deleted, because a permission against a menu that is gone can never be
      read back by the form and would silently re-appear if an id were reused.
   ============================================================================ */
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* ── 1. Institutes ──────────────────────────────────────────────────────── */

IF COL_LENGTH('dbo.Institutes', 'Tagline')         IS NULL ALTER TABLE dbo.Institutes ADD Tagline         NVARCHAR(400)  NULL;
GO
IF COL_LENGTH('dbo.Institutes', 'RegistrationNo')  IS NULL ALTER TABLE dbo.Institutes ADD RegistrationNo  NVARCHAR(100)  NULL;
GO
IF COL_LENGTH('dbo.Institutes', 'EstablishedYear') IS NULL ALTER TABLE dbo.Institutes ADD EstablishedYear INT            NULL;
GO
IF COL_LENGTH('dbo.Institutes', 'City')            IS NULL ALTER TABLE dbo.Institutes ADD City            NVARCHAR(200)  NULL;
GO
IF COL_LENGTH('dbo.Institutes', 'State')           IS NULL ALTER TABLE dbo.Institutes ADD [State]         NVARCHAR(200)  NULL;
GO
IF COL_LENGTH('dbo.Institutes', 'Country')         IS NULL ALTER TABLE dbo.Institutes ADD Country         NVARCHAR(200)  NULL;
GO
IF COL_LENGTH('dbo.Institutes', 'PostalCode')      IS NULL ALTER TABLE dbo.Institutes ADD PostalCode      NVARCHAR(40)   NULL;
GO
IF COL_LENGTH('dbo.Institutes', 'PrincipalName')   IS NULL ALTER TABLE dbo.Institutes ADD PrincipalName   NVARCHAR(300)  NULL;
GO
IF COL_LENGTH('dbo.Institutes', 'PrincipalPhone')  IS NULL ALTER TABLE dbo.Institutes ADD PrincipalPhone  NVARCHAR(40)   NULL;
GO
IF COL_LENGTH('dbo.Institutes', 'PrincipalEmail')  IS NULL ALTER TABLE dbo.Institutes ADD PrincipalEmail  NVARCHAR(400)  NULL;
GO

-- A logo held as a data URL does not fit in 1000 characters.
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('dbo.Institutes') AND name = 'LogoUrl' AND max_length <> -1)
    ALTER TABLE dbo.Institutes ALTER COLUMN LogoUrl NVARCHAR(MAX) NULL;
GO

-- The profile is one row. Seed it from what the school already prints on its
-- own documents, so the form opens filled in rather than blank.
IF NOT EXISTS (SELECT 1 FROM dbo.Institutes)
    INSERT dbo.Institutes (Name, Code, Address, City, Country, Phone, IsActive, CreatedAt)
    VALUES (N'SIR SYED MODEL HIGH SCHOOL (BOYS & GIRLS)', N'SSMHS',
            N'Chhonawala Road, Hasilpur', N'Hasilpur', N'Pakistan', N'0301-7330101', 1, GETDATE());
GO

/* ── 2. Menus ───────────────────────────────────────────────────────────── */

IF COL_LENGTH('dbo.Menus', 'RoutingPageUrl') IS NULL ALTER TABLE dbo.Menus ADD RoutingPageUrl NVARCHAR(400) NULL;
GO
IF COL_LENGTH('dbo.Menus', 'SrNo')           IS NULL ALTER TABLE dbo.Menus ADD SrNo           INT           NULL;
GO

/* Rebuild the menu list to match the application.

   Ids are FIXED and match SidebarController, so a permission granted today
   still means the same page after the next deployment. Sections are 1..8 and
   pages keep the ids the sidebar already gives them. */

IF OBJECT_ID('tempdb..#Menu') IS NOT NULL DROP TABLE #Menu;
CREATE TABLE #Menu (Id INT PRIMARY KEY, MenuTitle NVARCHAR(200), Url NVARCHAR(400),
                    Icon NVARCHAR(100), ParentId INT NULL, DisplayOrder INT, [Level] INT);

INSERT #Menu (Id, MenuTitle, Url, Icon, ParentId, DisplayOrder, [Level]) VALUES
-- sections (Level 1). Ids 1-8 are reserved for these.
 (1, N'Students',            NULL, N'bi-people-fill',            NULL, 1, 1),
 (2, N'Academics',           NULL, N'bi-mortarboard-fill',       NULL, 2, 1),
 (3, N'Examinations',        NULL, N'bi-clipboard2-data-fill',   NULL, 3, 1),
 (4, N'Fees',                NULL, N'bi-wallet-fill',            NULL, 4, 1),
 (5, N'Staff & Payroll',     NULL, N'bi-briefcase-fill',         NULL, 5, 1),
 (6, N'WhatsApp',            NULL, N'bi-whatsapp',               NULL, 6, 1),
 (7, N'Reports & Documents', NULL, N'bi-bar-chart-line-fill',    NULL, 7, 1),
 (8, N'Administration',      NULL, N'bi-person-gear',            NULL, 8, 1),

-- Students
 (10, N'New Admission',            N'/admission/form',    N'bi-file-earmark-plus',     1, 1, 2),
 (11, N'Students',                 N'/Student',           N'bi-person-vcard',          1, 2, 2),
 (12, N'Change Student Status',    N'/status-change',     N'bi-person-check-fill',     1, 3, 2),
 (13, N'Mark Daily Attendance',    N'/attendance/form',   N'bi-check2-square',         1, 4, 2),
 (14, N'Attendance Record',        N'/Attendance',        N'bi-calendar2-check-fill',  1, 5, 2),
 (15, N'Print Attendance Sheet',   N'/attendance/sheet',  N'bi-printer-fill',          1, 6, 2),

-- Academics
 (20, N'Campus',                   N'/Campus',            N'bi-buildings',             2, 1, 2),
 (21, N'Classes',                  N'/Classes',           N'bi-building',              2, 2, 2),
 (22, N'Sections',                 N'/Sections',          N'bi-diagram-3-fill',        2, 3, 2),
 (23, N'Subjects',                 N'/Subjects',          N'bi-journal-bookmark-fill', 2, 4, 2),
 (24, N'Class Timings (Students)', N'/class-timing',      N'bi-clock',                 2, 5, 2),
 (25, N'Subject Allocation',       N'/staff-subjects',    N'bi-person-workspace',      2, 6, 2),
 (28, N'Academic Sessions',        N'/Sessions',          N'bi-calendar-range',        2, 7, 2),
 (29, N'Grading Scale',            N'/AcademicGrade',     N'bi-award-fill',            2, 8, 2),
 (30, N'Student Status Types',     N'/student-status',    N'bi-person-check',          2, 9, 2),

-- Examinations
 (40, N'Exams',                    N'/Exam',              N'bi-clipboard2-data-fill',  3, 1, 2),
 (41, N'Marks Entry',              N'/exam/marks-entry',  N'bi-trophy-fill',           3, 2, 2),
 (42, N'Exam Reports',             N'/exam/reports',      N'bi-file-earmark-bar-graph-fill', 3, 3, 2),

-- Fees
 (50, N'Apply Monthly Fee',        N'/apply-fee',         N'bi-currency-dollar',       4, 1, 2),
 (51, N'Applied Fee Records',      N'/feeList',           N'bi-list-check',            4, 2, 2),
 (52, N'Receive Payment',          N'/FeeVoucher/form',   N'bi-receipt',               4, 3, 2),
 (53, N'Payment Records',          N'/FeeVoucher',        N'bi-journal-check',         4, 4, 2),
 (55, N'Fee Reports',              N'/fee-report',        N'bi-cash-coin',             4, 5, 2),
 (56, N'Student Ledger',           N'/ledger',            N'bi-journal-text',          4, 6, 2),
 (58, N'Fee Types',                N'/FeeType',           N'bi-tags-fill',             4, 7, 2),
 (59, N'Fee Structure',            N'/FeeStructure',      N'bi-clipboard-data',        4, 8, 2),
 (60, N'Payment Methods',          N'/PaymentMethod',     N'bi-credit-card-fill',      4, 9, 2),

-- Staff & Payroll
 (70, N'Employees',                N'/Employee',          N'bi-person-vcard-fill',     5, 1, 2),
 (71, N'Staff Attendance',         N'/hr/attendance',     N'bi-calendar2-check-fill',  5, 2, 2),
 (72, N'Leave',                    N'/hr/leave',          N'bi-calendar-minus-fill',   5, 3, 2),
 (73, N'Generate Payroll',         N'/hr/payroll',        N'bi-cash-stack',            5, 4, 2),
 (74, N'Staff Attendance Report',  N'/hr/reports/attendance',      N'bi-bar-chart-fill', 5, 5, 2),
 (75, N'Staff Statement',          N'/hr/reports/staff-statement', N'bi-people-fill',    5, 6, 2),
 (77, N'Staff Timings',            N'/hr/school-timing',  N'bi-clock-history',         5, 7, 2),
 (80, N'Payroll Setup',            N'/hr/setup',          N'bi-sliders',               5, 8, 2),

-- WhatsApp
 (90, N'Send Message',             N'/sms',               N'bi-send-fill',             6, 1, 2),
 (91, N'Message Log',              N'/sms/messages',      N'bi-list-ul',               6, 2, 2),
 (92, N'Chat',                     N'/sms/chat',          N'bi-chat-left-text-fill',   6, 3, 2),

-- Reports & Documents
 (100, N'Certificates',            N'/certificates',      N'bi-award-fill',            7, 1, 2),
 (101, N'Custom Reports',          N'/custom-report',     N'bi-sliders2',              7, 2, 2),

-- Administration
 (110, N'Users',                   N'/Users',             N'bi-people-fill',           8, 1, 2),
 (111, N'Roles & Permissions',     N'/Roles',             N'bi-shield-lock-fill',      8, 2, 2),
 (112, N'Page Permissions',        N'/Pages',             N'bi-file-earmark-text-fill',8, 3, 2),
 (113, N'School Profile',          N'/my-institute',      N'bi-building-gear',         8, 4, 2),
 (114, N'Backup & Restore',        N'/Settings/Backup',   N'bi-cloud-arrow-up-fill',   8, 5, 2);

SET IDENTITY_INSERT dbo.Menus ON;

-- Update what is already there, insert what is not, and retire the rest.
UPDATE m
   SET m.MenuTitle      = s.MenuTitle,
       m.Url            = s.Url,
       m.Icon           = s.Icon,
       m.ParentId       = s.ParentId,
       m.DisplayOrder   = s.DisplayOrder,
       m.[Level]        = s.[Level],
       m.RoutingPageUrl = s.Url,
       m.SrNo           = s.Id,
       m.IsActive       = 1,
       m.UpdatedAt      = GETDATE()
  FROM dbo.Menus m
  JOIN #Menu s ON s.Id = m.Id;

INSERT dbo.Menus (Id, MenuTitle, Url, Icon, ParentId, DisplayOrder, IsActive, [Level], CreatedAt, RoutingPageUrl, SrNo)
SELECT s.Id, s.MenuTitle, s.Url, s.Icon, s.ParentId, s.DisplayOrder, 1, s.[Level], GETDATE(), s.Url, s.Id
  FROM #Menu s
 WHERE NOT EXISTS (SELECT 1 FROM dbo.Menus m WHERE m.Id = s.Id);

SET IDENTITY_INSERT dbo.Menus OFF;

-- Rows left over from the starter template. Their permissions go with them:
-- a right granted on "Teacher Management" cannot be honoured by a screen that
-- was never built, and leaving it would hand that right to whatever page
-- happened to inherit the id later.
DELETE rp
  FROM dbo.RolePermissions rp
 WHERE NOT EXISTS (SELECT 1 FROM #Menu s WHERE s.Id = rp.MenuId);

DELETE FROM dbo.Menus
 WHERE NOT EXISTS (SELECT 1 FROM #Menu s WHERE s.Id = dbo.Menus.Id);

DROP TABLE #Menu;
GO

/* ── 3. SecUserRole — which user has which role ─────────────────────────── */

/* The Users form shows a Roles picker and has never saved it, because there
   was nowhere to save it TO.

   UserRoles looks like the right table but is not: its foreign key points at
   Users, the Entity-Framework user table, which holds one row. The application
   logs in against SecUser (AuthController -> SpLogin_SecUser), and that is the
   table the Users form creates and deletes. Writing a SecUser id into
   UserRoles would be rejected by that foreign key, and removing the key to
   force it would leave two tables claiming to define a user.

   So the link gets its own table, keyed to the user table the form actually
   edits. UserRoles is left exactly as it is. */

IF OBJECT_ID('dbo.SecUserRole', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SecUserRole
    (
        Id        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SecUserRole PRIMARY KEY,
        UserVID   INT NOT NULL,
        RoleId    INT NOT NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_SecUserRole_CreatedAt DEFAULT (GETDATE()),

        CONSTRAINT FK_SecUserRole_SecUser FOREIGN KEY (UserVID) REFERENCES dbo.SecUser (VID) ON DELETE CASCADE,
        CONSTRAINT FK_SecUserRole_Role    FOREIGN KEY (RoleId)  REFERENCES dbo.Roles (Id)    ON DELETE CASCADE
    );

    -- One row per user per role: saving the form twice must not double it up.
    CREATE UNIQUE INDEX UX_SecUserRole_User_Role ON dbo.SecUserRole (UserVID, RoleId);
END
GO

/* ── 4. What is now in place ────────────────────────────────────────────── */
SELECT 'Institute columns : ' + CAST(COUNT(*) AS VARCHAR) FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Institutes');
SELECT 'Institute rows    : ' + CAST(COUNT(*) AS VARCHAR) FROM dbo.Institutes;
SELECT 'Menu sections     : ' + CAST(COUNT(*) AS VARCHAR) FROM dbo.Menus WHERE ParentId IS NULL;
SELECT 'Menu pages        : ' + CAST(COUNT(*) AS VARCHAR) FROM dbo.Menus WHERE ParentId IS NOT NULL;
SELECT 'Role permissions  : ' + CAST(COUNT(*) AS VARCHAR) FROM dbo.RolePermissions;
SELECT 'SecUserRole table : ' + CASE WHEN OBJECT_ID('dbo.SecUserRole') IS NULL THEN 'missing' ELSE 'ready (' + CAST((SELECT COUNT(*) FROM dbo.SecUserRole) AS VARCHAR) + ' rows)' END;
GO

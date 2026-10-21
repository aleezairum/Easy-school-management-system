/* ============================================================================
   Staff categories - a way to group staff beyond their designation
   ============================================================================

   Deliberately NO "USE [EasySchoolDB]" here: a hard-coded USE overrides
   sqlcmd's -d and sends the script to whatever that name resolves to on the
   machine it runs on. The database comes from -d instead.

     cd School.API\Scripts
     sqlcmd -S . -U sa -P <pwd> -d EasySchoolDB -i StaffCategory.sql

   WHY, WHEN THERE IS ALREADY A DESIGNATION
   ----------------------------------------
   A designation is the job: Principal, Teacher, Coordinator, Office Incharge.
   A category is the group the office wants to count and print by: Teaching,
   Non-Teaching, Admin, Support - whatever the school decides. Several
   designations fall into one category, and the two lists change for different
   reasons, so they are kept apart.

   WHAT THIS CREATES

     HrStaffCategory          the list the office maintains from its own screen

     Employees.StaffCategoryId
                              nullable, because every member of staff already
                              on file predates the column and there is no
                              honest way to guess which category they belong
                              to. Null reads as "not set yet", and a category
                              filter simply does not match those rows until the
                              office says which category it is.

   Nothing is seeded. The categories are the school's to name; inventing
   "Teaching / Non-Teaching" here would only have to be undone.
   ============================================================================ */
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* ── 1. The list ────────────────────────────────────────────────────────── */

IF OBJECT_ID('dbo.HrStaffCategory', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HrStaffCategory
    (
        Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HrStaffCategory PRIMARY KEY,
        Code        NVARCHAR(20)  NOT NULL,
        Name        NVARCHAR(100) NOT NULL,
        Description NVARCHAR(255) NULL,
        SortOrder   INT           NULL,
        IsActive    BIT           NOT NULL CONSTRAINT DF_HrStaffCategory_IsActive DEFAULT (1),
        CreatedAt   DATETIME      NOT NULL CONSTRAINT DF_HrStaffCategory_Created  DEFAULT (GETDATE()),
        UpdatedAt   DATETIME      NULL
    );

    -- The code is what the office types and what a report groups by, so two
    -- categories cannot share one.
    CREATE UNIQUE INDEX UX_HrStaffCategory_Code ON dbo.HrStaffCategory (Code);
END
GO

/* ── 2. The link from a member of staff ─────────────────────────────────── */

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.Employees') AND name = 'StaffCategoryId')
BEGIN
    ALTER TABLE dbo.Employees ADD StaffCategoryId INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Employees_StaffCategory')
BEGIN
    ALTER TABLE dbo.Employees
        ADD CONSTRAINT FK_Employees_StaffCategory
        FOREIGN KEY (StaffCategoryId) REFERENCES dbo.HrStaffCategory (Id);
END
GO

/* NO INDEX ON Employees.StaffCategoryId, deliberately.

   This school has fewer than a hundred employees. A scan of that is free, and
   an index on a column nothing joins on would be one more object to keep. If
   the table ever grows enough to matter, add a plain index then - and a plain
   one, not a filtered one, because a filtered index makes a table's DML
   demand QUOTED_IDENTIFIER ON.

   The first version of this script did create a filtered index here; it is
   dropped below in case that version was already applied anywhere.

   WHILE YOU ARE HERE - a trap that predates this script entirely:
   Employees carries the computed column VName, and because of it SQL Server
   refuses EVERY update to this table from a connection with QUOTED_IDENTIFIER
   OFF:

     UPDATE failed because the following SET options have incorrect
     settings: 'QUOTED_IDENTIFIER'

   The application never sees it - SqlClient sets the option ON - but
   "sqlcmd -Q" does not. Any maintenance script that touches Employees must
   start with SET QUOTED_IDENTIFIER ON, as this one does. */
IF EXISTS (SELECT 1 FROM sys.indexes
           WHERE name = 'IX_Employees_StaffCategoryId' AND object_id = OBJECT_ID('dbo.Employees'))
BEGIN
    DROP INDEX IX_Employees_StaffCategoryId ON dbo.Employees;
END
GO

SELECT 'HrStaffCategory          : ' + CASE WHEN OBJECT_ID('dbo.HrStaffCategory') IS NULL THEN 'missing' ELSE 'ready' END;
SELECT 'Employees.StaffCategoryId: ' + CASE WHEN EXISTS (SELECT 1 FROM sys.columns
                                                          WHERE object_id = OBJECT_ID('dbo.Employees')
                                                            AND name = 'StaffCategoryId')
                                            THEN 'ready' ELSE 'missing' END;
SELECT 'categories on file       : ' + CAST((SELECT COUNT(*) FROM dbo.HrStaffCategory) AS VARCHAR);
GO

/* ============================================================================
   SMSTeacher  ->  Employees      (for a database that has NO Employees table)

   Use this one when the HR module was never installed: SMSTeacher holds the
   staff, and there is nothing to merge with. The table is grown to the full
   Employee shape and renamed, so every VID survives as Employees.Id —
   SMSStaffSubject.TeacherID, SMSClassTimetable.TeacherID and the exam reports
   keep resolving without a single row being rewritten.

   USE RenameTeacherToEmployee.sql INSTEAD when the database already has an
   Employees table to merge in (the development database did). That script
   additionally merges those rows, re-points HrAttendance / HrLeave /
   HrEmployeePay / HrPayslip / Users / TimeTables onto the surviving ids, and
   drops the old table. None of that applies here — there is no second table and
   no id to remap, which is why this is a separate script rather than more
   guards: a plain SQL batch will not even COMPILE against a table that does not
   exist, however well guarded.

   Steps 2-5 and 10-12 below are deliberately the same work, in the same order,
   as the same-numbered steps in that script. Keep them in step.

   ----------------------------------------------------------------------------
   >>> TAKE A FULL DATABASE BACKUP BEFORE RUNNING THIS. <<<

   Run order on a fresh database:
        THIS SCRIPT
        -> CoreHrTables_Fix.sql        (Designations, HRGrades, Employee FKs)
        -> HrPayroll.sql               (shifts, attendance, leave, payroll)
        -> HrSchoolTiming.sql
           AddFridayAndDayOverrides.sql
           AddOffDaysToSchoolTiming.sql
        -> AddExamToStaffSubject.sql
           SMSStaffSubject.sql
        -> SMSTeacher.sql              (drops the retired procedures)
           SMSClassTimetable.sql
           SpRPT_SMSExamExecutive.sql
           SpRPT_SMSExamSubjectWise.sql
           SpRPT_SMSExamTeacherWise.sql
           SpRPT_SMSExamTeacherComparison.sql
           SpRPT_SMSStudentFee.sql

   Idempotent: every batch is guarded on schema state. Once dbo.SMSTeacher is no
   longer a TABLE the migration batches are all no-ops.

   Target: SQL Server 2019
   ============================================================================ */

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ===========================================================================
   0. Refuse to run in the wrong situation.
   =========================================================================== */
IF OBJECT_ID('dbo.SMSTeacher', 'U') IS NULL
    PRINT '>> dbo.SMSTeacher is not a table - nothing to rename. All steps will no-op.';
ELSE IF OBJECT_ID('dbo.Employees', 'U') IS NOT NULL
BEGIN
    RAISERROR('This database already has an Employees table. Use RenameTeacherToEmployee.sql, which merges it, instead of this script.', 16, 1);
    SET NOEXEC ON;
END
ELSE
BEGIN
    DECLARE @t INT;
    SELECT @t = COUNT(*) FROM dbo.SMSTeacher;
    PRINT '>> Migrating. SMSTeacher rows: ' + CAST(@t AS VARCHAR(10));
END
GO


/* ===========================================================================
   2. Grow SMSTeacher to the full Employee column set.

   Columns SMSTeacher already has (FatherName, CNIC, Mobile, Email, Address,
   Qualification, IsActive and the audit columns) are left alone here and
   normalised at step 3.
   =========================================================================== */
IF OBJECT_ID('dbo.SMSTeacher', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.SMSTeacher','EmployeeCode')             IS NULL ALTER TABLE dbo.SMSTeacher ADD EmployeeCode             NVARCHAR(50)  NULL;
    IF COL_LENGTH('dbo.SMSTeacher','FirstName')                IS NULL ALTER TABLE dbo.SMSTeacher ADD FirstName                NVARCHAR(100) NULL;
    IF COL_LENGTH('dbo.SMSTeacher','LastName')                 IS NULL ALTER TABLE dbo.SMSTeacher ADD LastName                 NVARCHAR(100) NULL;
    IF COL_LENGTH('dbo.SMSTeacher','FirstNameUrdu')            IS NULL ALTER TABLE dbo.SMSTeacher ADD FirstNameUrdu            NVARCHAR(100) NULL;
    IF COL_LENGTH('dbo.SMSTeacher','LastNameUrdu')             IS NULL ALTER TABLE dbo.SMSTeacher ADD LastNameUrdu             NVARCHAR(100) NULL;
    IF COL_LENGTH('dbo.SMSTeacher','GenderText')               IS NULL ALTER TABLE dbo.SMSTeacher ADD GenderText               NVARCHAR(10)  NULL;
    IF COL_LENGTH('dbo.SMSTeacher','Phone')                    IS NULL ALTER TABLE dbo.SMSTeacher ADD Phone                    NVARCHAR(20)  NULL;
    IF COL_LENGTH('dbo.SMSTeacher','DateOfBirth')              IS NULL ALTER TABLE dbo.SMSTeacher ADD DateOfBirth              DATETIME2(0)  NULL;
    IF COL_LENGTH('dbo.SMSTeacher','JoiningDate')              IS NULL ALTER TABLE dbo.SMSTeacher ADD JoiningDate              DATETIME2(0)  NULL;
    IF COL_LENGTH('dbo.SMSTeacher','LeavingDate')              IS NULL ALTER TABLE dbo.SMSTeacher ADD LeavingDate              DATETIME2(0)  NULL;
    IF COL_LENGTH('dbo.SMSTeacher','City')                     IS NULL ALTER TABLE dbo.SMSTeacher ADD City                     NVARCHAR(100) NULL;
    IF COL_LENGTH('dbo.SMSTeacher','Photo')                    IS NULL ALTER TABLE dbo.SMSTeacher ADD Photo                    NVARCHAR(255) NULL;
    IF COL_LENGTH('dbo.SMSTeacher','Specialization')           IS NULL ALTER TABLE dbo.SMSTeacher ADD Specialization           NVARCHAR(100) NULL;
    IF COL_LENGTH('dbo.SMSTeacher','Experience')               IS NULL ALTER TABLE dbo.SMSTeacher ADD Experience               NVARCHAR(100) NULL;
    IF COL_LENGTH('dbo.SMSTeacher','Department')               IS NULL ALTER TABLE dbo.SMSTeacher ADD Department               NVARCHAR(100) NULL;
    IF COL_LENGTH('dbo.SMSTeacher','DesignationId')            IS NULL ALTER TABLE dbo.SMSTeacher ADD DesignationId            INT           NULL;
    IF COL_LENGTH('dbo.SMSTeacher','HRGradeId')                IS NULL ALTER TABLE dbo.SMSTeacher ADD HRGradeId                INT           NULL;
    IF COL_LENGTH('dbo.SMSTeacher','AcademicGradeId')          IS NULL ALTER TABLE dbo.SMSTeacher ADD AcademicGradeId          INT           NULL;
    IF COL_LENGTH('dbo.SMSTeacher','ShiftId')                  IS NULL ALTER TABLE dbo.SMSTeacher ADD ShiftId                  INT           NULL;
    IF COL_LENGTH('dbo.SMSTeacher','MachineCardNo')            IS NULL ALTER TABLE dbo.SMSTeacher ADD MachineCardNo            NVARCHAR(50)  NULL;
    IF COL_LENGTH('dbo.SMSTeacher','OffDay1')                  IS NULL ALTER TABLE dbo.SMSTeacher ADD OffDay1                  TINYINT       NULL;
    IF COL_LENGTH('dbo.SMSTeacher','OffDay2')                  IS NULL ALTER TABLE dbo.SMSTeacher ADD OffDay2                  TINYINT       NULL;
    IF COL_LENGTH('dbo.SMSTeacher','IsTeachingStaff')          IS NULL ALTER TABLE dbo.SMSTeacher ADD IsTeachingStaff          BIT           NOT NULL CONSTRAINT DF_Employees_IsTeachingStaff_New DEFAULT (0);
    IF COL_LENGTH('dbo.SMSTeacher','BankName')                 IS NULL ALTER TABLE dbo.SMSTeacher ADD BankName                 NVARCHAR(100) NULL;
    IF COL_LENGTH('dbo.SMSTeacher','BankAccountNo')            IS NULL ALTER TABLE dbo.SMSTeacher ADD BankAccountNo            NVARCHAR(50)  NULL;
    IF COL_LENGTH('dbo.SMSTeacher','EmergencyContactName')     IS NULL ALTER TABLE dbo.SMSTeacher ADD EmergencyContactName     NVARCHAR(100) NULL;
    IF COL_LENGTH('dbo.SMSTeacher','EmergencyContactPhone')    IS NULL ALTER TABLE dbo.SMSTeacher ADD EmergencyContactPhone    NVARCHAR(20)  NULL;
    IF COL_LENGTH('dbo.SMSTeacher','EmergencyContactRelation') IS NULL ALTER TABLE dbo.SMSTeacher ADD EmergencyContactRelation NVARCHAR(50)  NULL;
    IF COL_LENGTH('dbo.SMSTeacher','EmployeeType')             IS NULL ALTER TABLE dbo.SMSTeacher ADD EmployeeType             NVARCHAR(50)  NULL;
    IF COL_LENGTH('dbo.SMSTeacher','Status')                   IS NULL ALTER TABLE dbo.SMSTeacher ADD Status                   NVARCHAR(50)  NULL;

    PRINT '>> SMSTeacher grown to the Employee column set.';
END
GO


/* ===========================================================================
   3. Normalise the columns SMSTeacher already had.

   - VARCHAR -> NVARCHAR, so Urdu / non-ASCII names survive.
   - The audit columns take the Employee names: InsertedBy/Date/Ip become
     CreatedBy/CreatedAt/CreatedIp, UpdatedDate becomes UpdatedAt. No data is
     copied or lost - these are pure renames.
   =========================================================================== */
IF OBJECT_ID('dbo.SMSTeacher', 'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.SMSTeacher ALTER COLUMN FatherName    NVARCHAR(200) NULL;
    ALTER TABLE dbo.SMSTeacher ALTER COLUMN CNIC          NVARCHAR(20)  NULL;
    ALTER TABLE dbo.SMSTeacher ALTER COLUMN Mobile        NVARCHAR(20)  NULL;
    ALTER TABLE dbo.SMSTeacher ALTER COLUMN Email         NVARCHAR(200) NULL;
    ALTER TABLE dbo.SMSTeacher ALTER COLUMN Address       NVARCHAR(500) NULL;
    ALTER TABLE dbo.SMSTeacher ALTER COLUMN Qualification NVARCHAR(500) NULL;

    IF COL_LENGTH('dbo.SMSTeacher','InsertedBy')   IS NOT NULL AND COL_LENGTH('dbo.SMSTeacher','CreatedBy') IS NULL
        EXEC sp_rename 'dbo.SMSTeacher.InsertedBy',   'CreatedBy',  'COLUMN';
    IF COL_LENGTH('dbo.SMSTeacher','InsertedDate') IS NOT NULL AND COL_LENGTH('dbo.SMSTeacher','CreatedAt') IS NULL
        EXEC sp_rename 'dbo.SMSTeacher.InsertedDate', 'CreatedAt',  'COLUMN';
    IF COL_LENGTH('dbo.SMSTeacher','InsertedIp')   IS NOT NULL AND COL_LENGTH('dbo.SMSTeacher','CreatedIp') IS NULL
        EXEC sp_rename 'dbo.SMSTeacher.InsertedIp',   'CreatedIp',  'COLUMN';
    IF COL_LENGTH('dbo.SMSTeacher','UpdatedDate')  IS NOT NULL AND COL_LENGTH('dbo.SMSTeacher','UpdatedAt') IS NULL
        EXEC sp_rename 'dbo.SMSTeacher.UpdatedDate',  'UpdatedAt',  'COLUMN';

    PRINT '>> Legacy columns normalised.';
END
GO

IF OBJECT_ID('dbo.SMSTeacher', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.SMSTeacher','CreatedBy') IS NULL ALTER TABLE dbo.SMSTeacher ADD CreatedBy INT           NULL;
    IF COL_LENGTH('dbo.SMSTeacher','CreatedAt') IS NULL ALTER TABLE dbo.SMSTeacher ADD CreatedAt DATETIME2(0)  NULL;
    IF COL_LENGTH('dbo.SMSTeacher','CreatedIp') IS NULL ALTER TABLE dbo.SMSTeacher ADD CreatedIp NVARCHAR(200) NULL;
    IF COL_LENGTH('dbo.SMSTeacher','UpdatedBy') IS NULL ALTER TABLE dbo.SMSTeacher ADD UpdatedBy INT           NULL;
    IF COL_LENGTH('dbo.SMSTeacher','UpdatedAt') IS NULL ALTER TABLE dbo.SMSTeacher ADD UpdatedAt DATETIME2(0)  NULL;
    IF COL_LENGTH('dbo.SMSTeacher','UpdatedIp') IS NULL ALTER TABLE dbo.SMSTeacher ADD UpdatedIp NVARCHAR(200) NULL;
END
GO


/* ===========================================================================
   4. Fill the new columns from SMSTeacher's own legacy ones.

   VName is split on the FIRST space, so FirstName + ' ' + LastName rebuilds it
   character for character. Gender uses the teacher form's codes (1 = Male,
   2 = Female); that dropdown had no blank option and defaulted to Male, so
   treat migrated gender as unverified and correct it from the Employee screen.
   =========================================================================== */
IF OBJECT_ID('dbo.SMSTeacher', 'U') IS NOT NULL AND COL_LENGTH('dbo.SMSTeacher','VName') IS NOT NULL
BEGIN
    UPDATE dbo.SMSTeacher
    SET FirstName = CASE WHEN CHARINDEX(' ', LTRIM(RTRIM(VName))) = 0
                         THEN LTRIM(RTRIM(VName))
                         ELSE LEFT(LTRIM(RTRIM(VName)), CHARINDEX(' ', LTRIM(RTRIM(VName))) - 1)
                    END,
        LastName  = CASE WHEN CHARINDEX(' ', LTRIM(RTRIM(VName))) = 0
                         THEN NULL
                         ELSE SUBSTRING(LTRIM(RTRIM(VName)), CHARINDEX(' ', LTRIM(RTRIM(VName))) + 1, 100)
                    END
    WHERE FirstName IS NULL;

    UPDATE dbo.SMSTeacher
    SET GenderText = CASE Gender WHEN 1 THEN 'Male' WHEN 2 THEN 'Female' ELSE NULL END
    WHERE GenderText IS NULL;

    UPDATE dbo.SMSTeacher
    SET Experience = CAST(ExperienceYears AS NVARCHAR(100))
    WHERE Experience IS NULL AND ExperienceYears IS NOT NULL;

    /* Every existing row came from the teacher screen, so it is teaching staff. */
    UPDATE dbo.SMSTeacher SET IsTeachingStaff = 1 WHERE IsTeachingStaff = 0;

    UPDATE dbo.SMSTeacher
    SET EmployeeCode = 'T' + RIGHT('000' + CAST(VID AS VARCHAR(10)), 3)
    WHERE EmployeeCode IS NULL;

    UPDATE dbo.SMSTeacher SET Status    = CASE WHEN ISNULL(IsActive, 1) = 1 THEN 'Active' ELSE 'Inactive' END WHERE Status IS NULL;
    UPDATE dbo.SMSTeacher SET CreatedAt = SYSUTCDATETIME() WHERE CreatedAt IS NULL;

    PRINT '>> Names, gender, experience and codes derived from the legacy columns.';
END
GO


/* ===========================================================================
   5. Retire the legacy shapes.

   VName comes back at step 11 as a PERSISTED computed column, so anything still
   selecting T.VName keeps reading the right name - it is simply no longer a
   place where a second copy of the name can drift.
   =========================================================================== */
IF OBJECT_ID('dbo.SMSTeacher', 'U') IS NOT NULL
BEGIN
    /* A column cannot be dropped while a default constraint or a non-clustered
       index still hangs off it, and this table predates the repo. */
    DECLARE @cleanup NVARCHAR(MAX) = N'';

    SELECT @cleanup = @cleanup
                    + N'ALTER TABLE dbo.SMSTeacher DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';' + CHAR(10)
    FROM   sys.default_constraints dc
    JOIN   sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    WHERE  dc.parent_object_id = OBJECT_ID('dbo.SMSTeacher')
      AND  c.name IN ('Gender', 'ExperienceYears', 'VName');

    SELECT @cleanup = @cleanup
                    + N'DROP INDEX ' + QUOTENAME(i.name) + N' ON dbo.SMSTeacher;' + CHAR(10)
    FROM  (SELECT DISTINCT i.name
           FROM   sys.indexes       i
           JOIN   sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
           JOIN   sys.columns       c  ON c.object_id  = i.object_id AND c.column_id = ic.column_id
           WHERE  i.object_id = OBJECT_ID('dbo.SMSTeacher')
             AND  i.is_primary_key = 0
             AND  i.is_unique_constraint = 0
             AND  c.name IN ('Gender', 'ExperienceYears', 'VName')) i;

    IF @cleanup <> N''
    BEGIN
        PRINT '>> Clearing what is attached to the columns being dropped:';
        PRINT @cleanup;
        EXEC sp_executesql @cleanup;
    END

    IF COL_LENGTH('dbo.SMSTeacher','Gender') IS NOT NULL AND COL_LENGTH('dbo.SMSTeacher','GenderText') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.SMSTeacher DROP COLUMN Gender;
        EXEC sp_rename 'dbo.SMSTeacher.GenderText', 'Gender', 'COLUMN';
    END

    IF COL_LENGTH('dbo.SMSTeacher','ExperienceYears') IS NOT NULL
        ALTER TABLE dbo.SMSTeacher DROP COLUMN ExperienceYears;

    IF COL_LENGTH('dbo.SMSTeacher','VName') IS NOT NULL
        ALTER TABLE dbo.SMSTeacher DROP COLUMN VName;

    PRINT '>> Legacy Gender/ExperienceYears/VName columns retired.';
END
GO


/* ===========================================================================
   10. The rename itself: VID becomes Id, SMSTeacher becomes Employees.

   sp_rename keeps the IDENTITY property and every stored value, which is the
   whole point - SMSStaffSubject.TeacherID and SMSClassTimetable.TeacherID still
   resolve, without a single row being touched.
   =========================================================================== */
IF OBJECT_ID('dbo.SMSTeacher', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.SMSTeacher','Id') IS NULL
        EXEC sp_rename 'dbo.SMSTeacher.VID', 'Id', 'COLUMN';

    EXEC sp_rename 'dbo.SMSTeacher', 'Employees';
    PRINT '>> SMSTeacher renamed to Employees (VID -> Id).';
END
GO


/* ===========================================================================
   11. Constraints, defaults, indexes and the VName compatibility column.
   =========================================================================== */
/* Guarded on VName being absent, which is true exactly once: the next batch
   adds it. Re-running these ALTER COLUMNs after the unique constraint exists
   would fail with "the object is dependent on column EmployeeCode". */
IF OBJECT_ID('dbo.Employees', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Employees','Id')    IS NOT NULL
   AND COL_LENGTH('dbo.Employees','VName') IS NULL
BEGIN
    UPDATE e
    SET    e.EmployeeCode = LEFT(e.EmployeeCode, 44) + '-' + CAST(e.Id AS NVARCHAR(10))
    FROM   dbo.Employees e
    JOIN  (SELECT EmployeeCode FROM dbo.Employees
           WHERE EmployeeCode IS NOT NULL GROUP BY EmployeeCode HAVING COUNT(*) > 1) d
           ON d.EmployeeCode = e.EmployeeCode;

    UPDATE dbo.Employees SET EmployeeCode = 'EMP' + RIGHT('000' + CAST(Id AS VARCHAR(10)), 3) WHERE EmployeeCode IS NULL;
    UPDATE dbo.Employees SET FirstName    = 'Unnamed'        WHERE FirstName IS NULL OR LTRIM(RTRIM(FirstName)) = '';
    UPDATE dbo.Employees SET CreatedAt    = SYSUTCDATETIME() WHERE CreatedAt IS NULL;

    /* CreatedAt arrives carrying InsertedDate's auto-named default constraint,
       and ALTER COLUMN refuses to touch a column something else depends on.
       Drop it here; the next batch puts a properly named one back. */
    DECLARE @dropDf NVARCHAR(MAX) = N'';

    SELECT @dropDf = @dropDf
                   + N'ALTER TABLE dbo.Employees DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';' + CHAR(10)
    FROM   sys.default_constraints dc
    JOIN   sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    WHERE  dc.parent_object_id = OBJECT_ID('dbo.Employees')
      AND  c.name IN ('EmployeeCode', 'FirstName', 'CreatedAt');

    IF @dropDf <> N''
    BEGIN
        PRINT '>> Dropping inherited defaults so the columns can be altered:';
        PRINT @dropDf;
        EXEC sp_executesql @dropDf;
    END

    ALTER TABLE dbo.Employees ALTER COLUMN EmployeeCode NVARCHAR(50)  NOT NULL;
    ALTER TABLE dbo.Employees ALTER COLUMN FirstName    NVARCHAR(100) NOT NULL;
    ALTER TABLE dbo.Employees ALTER COLUMN CreatedAt    DATETIME2(0)  NOT NULL;
END
GO

IF OBJECT_ID('dbo.Employees', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_Employees_EmployeeCode')
        ALTER TABLE dbo.Employees ADD CONSTRAINT UQ_Employees_EmployeeCode UNIQUE (EmployeeCode);

    /* Checked per COLUMN, not per constraint name: SMSTeacher may already carry
       a default on the column under a name of its own, and a second one is an
       error rather than a no-op. */
    IF NOT EXISTS (SELECT 1 FROM sys.default_constraints
                   WHERE parent_object_id = OBJECT_ID('dbo.Employees')
                     AND parent_column_id = COLUMNPROPERTY(OBJECT_ID('dbo.Employees'), 'Status', 'ColumnId'))
        ALTER TABLE dbo.Employees ADD CONSTRAINT DF_Employees_Status DEFAULT ('Active') FOR Status;

    IF NOT EXISTS (SELECT 1 FROM sys.default_constraints
                   WHERE parent_object_id = OBJECT_ID('dbo.Employees')
                     AND parent_column_id = COLUMNPROPERTY(OBJECT_ID('dbo.Employees'), 'CreatedAt', 'ColumnId'))
        ALTER TABLE dbo.Employees ADD CONSTRAINT DF_Employees_CreatedAt DEFAULT (SYSUTCDATETIME()) FOR CreatedAt;

    /* VName as a persisted computed column: every SP that still selects it keeps
       working, and it can no longer disagree with FirstName/LastName. */
    IF COL_LENGTH('dbo.Employees','VName') IS NULL
        ALTER TABLE dbo.Employees ADD VName AS
            (LTRIM(RTRIM(ISNULL(FirstName, '') + ' ' + ISNULL(LastName, '')))) PERSISTED;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_Teaching' AND object_id = OBJECT_ID('dbo.Employees'))
        CREATE INDEX IX_Employees_Teaching ON dbo.Employees (IsTeachingStaff, IsActive) INCLUDE (FirstName, LastName);

    PRINT '>> Constraints, defaults, VName computed column and indexes in place.';
END
GO

/* The Designation / HRGrade / Shift foreign keys are added by
   CoreHrTables_Fix.sql and HrPayroll.sql, which create those tables. */


/* ===========================================================================
   12. Retire the SMSTeacher stored procedures, and leave a read-only view
       behind under the old name.

   The view is a safety net for any stored procedure in the live database that
   this repo does not contain. Once you have confirmed nothing selects from it,
   drop it:   DROP VIEW dbo.SMSTeacher;
   =========================================================================== */
IF OBJECT_ID('dbo.SpSave_SMSTeacher',   'P') IS NOT NULL DROP PROCEDURE dbo.SpSave_SMSTeacher;
IF OBJECT_ID('dbo.SpDelete_SMSTeacher', 'P') IS NOT NULL DROP PROCEDURE dbo.SpDelete_SMSTeacher;
IF OBJECT_ID('dbo.SpGet_SMSTeacher',    'P') IS NOT NULL DROP PROCEDURE dbo.SpGet_SMSTeacher;
GO

IF OBJECT_ID('dbo.SMSTeacher', 'V') IS NOT NULL DROP VIEW dbo.SMSTeacher;
GO
IF OBJECT_ID('dbo.Employees', 'U') IS NOT NULL AND OBJECT_ID('dbo.SMSTeacher', 'U') IS NULL
    EXEC('
CREATE VIEW dbo.SMSTeacher
AS
    /* Compatibility only. Reads through to Employees; the table is gone. */
    SELECT  Id             AS VID,
            VName,
            FatherName,
            CASE Gender WHEN ''Male'' THEN 1 WHEN ''Female'' THEN 2 ELSE 0 END AS Gender,
            CNIC, Mobile, Email, Address, Qualification,
            TRY_CAST(Experience AS INT) AS ExperienceYears,
            IsActive,
            CreatedBy AS InsertedBy, CreatedAt AS InsertedDate, CreatedIp AS InsertedIp,
            UpdatedBy, UpdatedAt AS UpdatedDate, UpdatedIp
    FROM    dbo.Employees
    WHERE   IsTeachingStaff = 1;');
GO


/* ===========================================================================
   Result
   =========================================================================== */
SELECT 'Employees (was SMSTeacher)'  AS TableName,       COUNT(*) AS [Count] FROM dbo.Employees
UNION ALL SELECT 'Teaching staff',                       COUNT(*) FROM dbo.Employees WHERE IsTeachingStaff = 1
UNION ALL SELECT 'SMSStaffSubject rows',                 COUNT(*) FROM dbo.SMSStaffSubject
UNION ALL SELECT 'SMSStaffSubject with no employee (0)', COUNT(*) FROM dbo.SMSStaffSubject s
          WHERE NOT EXISTS (SELECT 1 FROM dbo.Employees e WHERE e.Id = s.TeacherID);
GO

SET NOEXEC OFF;
GO

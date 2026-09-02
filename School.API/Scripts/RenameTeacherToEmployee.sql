/* ============================================================================
   SMSTeacher  ->  Employees        (one staff table, one identity)

   BEFORE: two tables held the same people.
             SMSTeacher (VID)  = academic identity. SMSStaffSubject,
                                 SMSClassTimetable and every SpRPT_SMSExam*
                                 report join on it.
             Employees  (Id)   = HR identity. HrAttendance, HrLeave,
                                 HrEmployeePay, HrPayslip and Users join on it.
             Employees.TeacherId tied the two together and EmployeeService
                                 mirrored every save into SMSTeacher.

   AFTER:  ONE table, dbo.Employees, whose Id IS the old SMSTeacher.VID.
             - Academic joins keep working untouched, because the key values
               never change: SMSStaffSubject.TeacherID = Employees.Id.
             - HR joins are re-pointed by this script: every EmployeeId in
               Users / HrAttendance / HrLeave / HrEmployeePay / HrPayslip /
               TimeTables is rewritten from the old Employees.Id to the
               surviving Id.
             - The mirroring code is gone. There is nothing left to keep in
               sync, because there is only one row per person.

   HOW: the SMSTeacher table is grown to the full Employee column set, the old
        Employees table is merged into it, and only THEN is it renamed. The
        table object -- and therefore every VID value in it -- survives; it is
        the Employees table that is retired.

        Non-teaching employees (TeacherId IS NULL) had no SMSTeacher row, so
        they are inserted and get a fresh Id; that remap is what dbo.__EmpIdMap
        records.

   ----------------------------------------------------------------------------
   >>> TAKE A FULL DATABASE BACKUP BEFORE RUNNING THIS. <<<

   Run order:  CreateTables.sql -> CoreHrTables_Fix.sql -> HrPayroll.sql
               -> MergeTeacherIntoEmployee.sql -> THIS SCRIPT
               -> re-run SMSStaffSubject.sql and the four SpRPT_SMSExam*
                  scripts that join staff (they now read dbo.Employees).

   Idempotent: every batch is guarded on schema state. Once dbo.SMSTeacher is
   no longer a TABLE the migration batches are all no-ops, so a second run is
   harmless.

   Target: SQL Server 2019
   ============================================================================ */

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;   -- HrAttendance carries a filtered index; DML fails without it
GO

/* ===========================================================================
   0. Where are we?
   =========================================================================== */
IF OBJECT_ID('dbo.SMSTeacher', 'U') IS NULL
    PRINT '>> dbo.SMSTeacher is not a table - migration already done (or never applied). All steps will no-op.';
ELSE
BEGIN
    DECLARE @t INT = (SELECT COUNT(*) FROM dbo.SMSTeacher);
    DECLARE @e INT = (SELECT COUNT(*) FROM dbo.Employees);
    PRINT '>> Migrating. Row counts before:';
    PRINT '   SMSTeacher : ' + CAST(@t AS VARCHAR(10));
    PRINT '   Employees  : ' + CAST(@e AS VARCHAR(10));
END
GO


/* ===========================================================================
   1. Park the old Employees table out of the way.

   Every FK touching it is dropped first: the child rows are about to point at
   different Ids, and the constraints are recreated against the new table at
   step 11 under their original names.
   =========================================================================== */
IF OBJECT_ID('dbo.SMSTeacher', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.Employees', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.Employees_Old', 'U') IS NULL
BEGIN
    DECLARE @drop NVARCHAR(MAX) = N'';

    SELECT @drop = @drop
                 + N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
                 + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
    FROM   sys.foreign_keys fk
    JOIN   sys.tables       t ON t.object_id = fk.parent_object_id
    WHERE  fk.referenced_object_id = OBJECT_ID('dbo.Employees')
       OR  fk.parent_object_id     = OBJECT_ID('dbo.Employees');

    IF @drop <> N''
    BEGIN
        PRINT '>> Dropping foreign keys on/around Employees:';
        PRINT @drop;
        EXEC sp_executesql @drop;
    END

    EXEC sp_rename 'dbo.Employees', 'Employees_Old';
    PRINT '>> Employees renamed to Employees_Old.';
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

   - VARCHAR -> NVARCHAR, so Urdu / non-ASCII names survive (the Employee form
     has dedicated Urdu name fields).
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

    PRINT '>> Legacy columns normalised (types widened to NVARCHAR, audit columns renamed).';
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

   VName is split on the FIRST space, the same rule MergeTeacherIntoEmployee.sql
   used, so FirstName + ' ' + LastName rebuilds VName character for character.
   Gender uses the teacher form's codes (1 = Male, 2 = Female); note that in the
   original data almost every row sits on the dropdown's default of 1, so treat
   migrated gender as unverified and correct it from the Employee screen.
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

   VName comes back at step 11 as a PERSISTED computed column, so anything
   still selecting T.VName keeps reading the right name - it is simply no
   longer a place where a second copy of the name can drift.
   =========================================================================== */
IF OBJECT_ID('dbo.SMSTeacher', 'U') IS NOT NULL
BEGIN
    /* A column cannot be dropped while a default constraint or a non-clustered
       index still hangs off it, and this table predates the repo - so clear
       whatever is attached to the three columns on their way out. */
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
   6. Merge the old Employees rows that ALREADY have a teacher record.

   The Employee screen is where staff are edited, so on any column the two
   tables share, the Employees_Old value wins and the teacher value is only a
   fallback. The column list is computed from the two tables, so a column added
   to either one later is picked up without editing this script.
   =========================================================================== */
IF OBJECT_ID('dbo.SMSTeacher', 'U') IS NOT NULL AND OBJECT_ID('dbo.Employees_Old', 'U') IS NOT NULL
BEGIN
    /* Two employees pointing at one teacher would silently merge into a single
       row and take one of them down with it. Stop instead. */
    IF EXISTS (SELECT TeacherId FROM dbo.Employees_Old
               WHERE TeacherId IS NOT NULL GROUP BY TeacherId HAVING COUNT(*) > 1)
    BEGIN
        RAISERROR('Two or more rows in Employees_Old share a TeacherId. Resolve the duplicates before migrating.', 16, 1);
    END
    ELSE
    BEGIN
        DECLARE @set NVARCHAR(MAX) = N'';

        SELECT @set = @set + CASE WHEN @set = N'' THEN N'' ELSE N',' + CHAR(10) + N'        ' END
                    + N't.' + QUOTENAME(c.name) + N' = COALESCE(o.' + QUOTENAME(c.name) + N', t.' + QUOTENAME(c.name) + N')'
        FROM   sys.columns c
        WHERE  c.object_id   = OBJECT_ID('dbo.SMSTeacher')
          AND  c.is_identity = 0
          AND  c.is_computed = 0
          AND  c.name NOT IN ('VID', 'CreatedBy', 'CreatedIp', 'UpdatedBy', 'UpdatedIp')
          AND  EXISTS (SELECT 1 FROM sys.columns o
                       WHERE o.object_id = OBJECT_ID('dbo.Employees_Old') AND o.name = c.name);

        DECLARE @merge NVARCHAR(MAX) =
            N'UPDATE t SET' + CHAR(10) + N'        ' + @set + CHAR(10) +
            N'FROM dbo.SMSTeacher t JOIN dbo.Employees_Old o ON o.TeacherId = t.VID;';

        PRINT '>> Merging Employees_Old into the teacher rows:';
        PRINT @merge;
        EXEC sp_executesql @merge;
        PRINT '   rows merged: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
    END
END
GO


/* ===========================================================================
   7. Bring across the employees that never had a teacher record, and record
      the Id they move to.

   These are the non-teaching staff. They get a brand new identity value, so
   dbo.__EmpIdMap has to carry every employee - moved or not - for step 8.
   =========================================================================== */
IF OBJECT_ID('dbo.SMSTeacher', 'U') IS NOT NULL AND OBJECT_ID('dbo.Employees_Old', 'U') IS NOT NULL
BEGIN
    IF OBJECT_ID('dbo.__EmpIdMap', 'U') IS NULL
        CREATE TABLE dbo.__EmpIdMap
        (
            OldId INT          NULL,
            NewId INT          NOT NULL,
            Code  NVARCHAR(50) NULL
        );

    /* Teaching staff keep the teacher's own Id. */
    INSERT INTO dbo.__EmpIdMap (OldId, NewId, Code)
    SELECT o.Id, o.TeacherId, o.EmployeeCode
    FROM   dbo.Employees_Old o
    JOIN   dbo.SMSTeacher    t ON t.VID = o.TeacherId
    WHERE  NOT EXISTS (SELECT 1 FROM dbo.__EmpIdMap m WHERE m.OldId = o.Id);

    /* Everyone else is inserted; identity hands out the new Id, and the code
       (unique in Employees_Old) is what ties it back to the old row. */
    DECLARE @cols NVARCHAR(MAX) = N'';

    SELECT @cols = @cols + CASE WHEN @cols = N'' THEN N'' ELSE N', ' END + QUOTENAME(c.name)
    FROM   sys.columns c
    WHERE  c.object_id   = OBJECT_ID('dbo.SMSTeacher')
      AND  c.is_identity = 0
      AND  c.is_computed = 0
      AND  c.name <> 'VID'
      AND  EXISTS (SELECT 1 FROM sys.columns o
                   WHERE o.object_id = OBJECT_ID('dbo.Employees_Old') AND o.name = c.name);

    DECLARE @ins NVARCHAR(MAX) =
        N'INSERT INTO dbo.SMSTeacher (' + @cols + N')' + CHAR(10) +
        N'OUTPUT inserted.VID, inserted.EmployeeCode INTO dbo.__EmpIdMap (NewId, Code)' + CHAR(10) +
        N'SELECT ' + @cols + CHAR(10) +
        N'FROM dbo.Employees_Old o' + CHAR(10) +
        N'WHERE NOT EXISTS (SELECT 1 FROM dbo.SMSTeacher t WHERE t.VID = o.TeacherId)' + CHAR(10) +
        N'  AND NOT EXISTS (SELECT 1 FROM dbo.__EmpIdMap m WHERE m.OldId = o.Id);';

    PRINT '>> Bringing non-teaching employees across:';
    PRINT @ins;
    EXEC sp_executesql @ins;
    PRINT '   rows inserted: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

    UPDATE m
    SET    m.OldId = o.Id
    FROM   dbo.__EmpIdMap    m
    JOIN   dbo.Employees_Old o ON o.EmployeeCode = m.Code
    WHERE  m.OldId IS NULL;
END
GO


/* ===========================================================================
   8. Re-point every child row at the surviving Id.

   TimeTables is in this list on purpose: its TeacherId was being filled from
   the Employee dropdown (Employees.Id) while SMSClassTimetable and the exam
   reports used SMSTeacher.VID. After this the two agree, because there is only
   one Id left. SMSStaffSubject and SMSClassTimetable are NOT remapped - they
   already hold VID values, which are exactly the Ids that survive.
   =========================================================================== */
IF OBJECT_ID('dbo.SMSTeacher', 'U') IS NOT NULL AND OBJECT_ID('dbo.__EmpIdMap', 'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT NewId FROM dbo.__EmpIdMap GROUP BY NewId HAVING COUNT(*) > 1)
        RAISERROR('__EmpIdMap maps two old employees onto one new Id - refusing to remap.', 16, 1);
    ELSE IF EXISTS (SELECT 1 FROM dbo.Employees_Old o
                    WHERE NOT EXISTS (SELECT 1 FROM dbo.__EmpIdMap m WHERE m.OldId = o.Id))
        RAISERROR('Some Employees_Old rows have no entry in __EmpIdMap - refusing to remap.', 16, 1);
    ELSE
    BEGIN
        SET XACT_ABORT ON;
        BEGIN TRANSACTION;

            UPDATE u SET u.EmployeeId = m.NewId
            FROM dbo.Users u JOIN dbo.__EmpIdMap m ON m.OldId = u.EmployeeId;
            PRINT '   Users remapped         : ' + CAST(@@ROWCOUNT AS VARCHAR(10));

            IF OBJECT_ID('dbo.HrAttendance','U') IS NOT NULL
            BEGIN
                UPDATE a SET a.EmployeeId = m.NewId
                FROM dbo.HrAttendance a JOIN dbo.__EmpIdMap m ON m.OldId = a.EmployeeId;
                PRINT '   HrAttendance remapped  : ' + CAST(@@ROWCOUNT AS VARCHAR(10));
            END

            IF OBJECT_ID('dbo.HrLeave','U') IS NOT NULL
            BEGIN
                UPDATE l SET l.EmployeeId = m.NewId
                FROM dbo.HrLeave l JOIN dbo.__EmpIdMap m ON m.OldId = l.EmployeeId;
                PRINT '   HrLeave remapped       : ' + CAST(@@ROWCOUNT AS VARCHAR(10));
            END

            IF OBJECT_ID('dbo.HrEmployeePay','U') IS NOT NULL
            BEGIN
                UPDATE p SET p.EmployeeId = m.NewId
                FROM dbo.HrEmployeePay p JOIN dbo.__EmpIdMap m ON m.OldId = p.EmployeeId;
                PRINT '   HrEmployeePay remapped : ' + CAST(@@ROWCOUNT AS VARCHAR(10));
            END

            IF OBJECT_ID('dbo.HrPayslip','U') IS NOT NULL
            BEGIN
                UPDATE s SET s.EmployeeId = m.NewId
                FROM dbo.HrPayslip s JOIN dbo.__EmpIdMap m ON m.OldId = s.EmployeeId;
                PRINT '   HrPayslip remapped     : ' + CAST(@@ROWCOUNT AS VARCHAR(10));
            END

            IF OBJECT_ID('dbo.TimeTables','U') IS NOT NULL
            BEGIN
                UPDATE tt SET tt.TeacherId = m.NewId
                FROM dbo.TimeTables tt JOIN dbo.__EmpIdMap m ON m.OldId = tt.TeacherId;
                PRINT '   TimeTables remapped    : ' + CAST(@@ROWCOUNT AS VARCHAR(10));
            END

        COMMIT TRANSACTION;
        PRINT '>> Child rows re-pointed.';
    END
END
GO


/* ===========================================================================
   9. Drop the old table. Everything it held now lives in SMSTeacher.
   =========================================================================== */
IF OBJECT_ID('dbo.SMSTeacher', 'U') IS NOT NULL AND OBJECT_ID('dbo.Employees_Old', 'U') IS NOT NULL
BEGIN
    DECLARE @dropOld NVARCHAR(MAX) = N'';

    SELECT @dropOld = @dropOld
                    + N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
                    + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
    FROM   sys.foreign_keys fk
    JOIN   sys.tables       t ON t.object_id = fk.parent_object_id
    WHERE  fk.referenced_object_id = OBJECT_ID('dbo.Employees_Old')
       OR  fk.parent_object_id     = OBJECT_ID('dbo.Employees_Old');

    IF @dropOld <> N'' EXEC sp_executesql @dropOld;

    DROP TABLE dbo.Employees_Old;
    PRINT '>> Employees_Old dropped.';
END
GO


/* ===========================================================================
   10. The rename itself: VID becomes Id, SMSTeacher becomes Employees.

   sp_rename keeps the IDENTITY property and every stored value, which is the
   whole point - SMSStaffSubject.TeacherID and SMSClassTimetable.TeacherID
   still resolve, without a single row being touched.
   =========================================================================== */
IF OBJECT_ID('dbo.SMSTeacher', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.SMSTeacher','TeacherId') IS NOT NULL
        ALTER TABLE dbo.SMSTeacher DROP COLUMN TeacherId;   -- self-referential now

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
    /* Codes have to be unique before the constraint goes on. A collision can
       only happen if a hand-entered code matched a generated T-code. */
    UPDATE e
    SET    e.EmployeeCode = LEFT(e.EmployeeCode, 44) + '-' + CAST(e.Id AS NVARCHAR(10))
    FROM   dbo.Employees e
    JOIN  (SELECT EmployeeCode FROM dbo.Employees
           WHERE EmployeeCode IS NOT NULL GROUP BY EmployeeCode HAVING COUNT(*) > 1) d
           ON d.EmployeeCode = e.EmployeeCode;

    UPDATE dbo.Employees SET EmployeeCode = 'EMP' + RIGHT('000' + CAST(Id AS VARCHAR(10)), 3) WHERE EmployeeCode IS NULL;
    UPDATE dbo.Employees SET FirstName    = 'Unnamed'        WHERE FirstName IS NULL OR LTRIM(RTRIM(FirstName)) = '';
    UPDATE dbo.Employees SET CreatedAt    = SYSUTCDATETIME() WHERE CreatedAt IS NULL;

    /* CreatedAt arrives carrying InsertedDate's auto-named default constraint
       (DF__SMSTeache__Inser__...), and ALTER COLUMN refuses to touch a column
       something else depends on. Drop it here; step 11b puts a properly named
       one back. */
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

    /* VName as a persisted computed column: every SP that still selects it
       keeps working, and it can no longer disagree with FirstName/LastName. */
    IF COL_LENGTH('dbo.Employees','VName') IS NULL
        ALTER TABLE dbo.Employees ADD VName AS
            (LTRIM(RTRIM(ISNULL(FirstName, '') + ' ' + ISNULL(LastName, '')))) PERSISTED;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_Teaching' AND object_id = OBJECT_ID('dbo.Employees'))
        CREATE INDEX IX_Employees_Teaching ON dbo.Employees (IsTeachingStaff, IsActive) INCLUDE (FirstName, LastName);

    PRINT '>> Constraints, defaults, VName computed column and indexes in place.';
END
GO

/* Foreign keys, back under their original names. */
IF OBJECT_ID('dbo.Employees', 'U') IS NOT NULL
BEGIN
    IF OBJECT_ID('dbo.Designations','U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Employees_Designations')
        ALTER TABLE dbo.Employees ADD CONSTRAINT FK_Employees_Designations FOREIGN KEY (DesignationId) REFERENCES dbo.Designations(Id);

    IF OBJECT_ID('dbo.HRGrades','U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Employees_HRGrades')
        ALTER TABLE dbo.Employees ADD CONSTRAINT FK_Employees_HRGrades FOREIGN KEY (HRGradeId) REFERENCES dbo.HRGrades(Id);

    IF OBJECT_ID('dbo.HrShift','U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Employees_HrShift')
        ALTER TABLE dbo.Employees ADD CONSTRAINT FK_Employees_HrShift FOREIGN KEY (ShiftId) REFERENCES dbo.HrShift(Id);

    IF OBJECT_ID('dbo.Users','U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Users_Employee')
        ALTER TABLE dbo.Users ADD CONSTRAINT FK_Users_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(Id) ON DELETE SET NULL;

    IF OBJECT_ID('dbo.HrAttendance','U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_HrAtt_Emp')
        ALTER TABLE dbo.HrAttendance ADD CONSTRAINT FK_HrAtt_Emp FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(Id);

    IF OBJECT_ID('dbo.HrLeave','U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_HrLv_Emp')
        ALTER TABLE dbo.HrLeave ADD CONSTRAINT FK_HrLv_Emp FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(Id);

    IF OBJECT_ID('dbo.HrEmployeePay','U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_HrEP_Emp')
        ALTER TABLE dbo.HrEmployeePay ADD CONSTRAINT FK_HrEP_Emp FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(Id);

    IF OBJECT_ID('dbo.HrPayslip','U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_HrPS_Emp')
        ALTER TABLE dbo.HrPayslip ADD CONSTRAINT FK_HrPS_Emp FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(Id);

    PRINT '>> Foreign keys restored.';
END
GO


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
   13. Result
   =========================================================================== */
IF OBJECT_ID('dbo.__EmpIdMap', 'U') IS NOT NULL
BEGIN
    PRINT '>> Id remap (dbo.__EmpIdMap) - keep it until the screens are verified, then DROP TABLE dbo.__EmpIdMap;';
    SELECT TOP (50) OldId, NewId, Code FROM dbo.__EmpIdMap ORDER BY OldId;
END
GO

SELECT 'Employees (was SMSTeacher)'  AS TableName,       COUNT(*) AS [Count] FROM dbo.Employees
UNION ALL SELECT 'Teaching staff',                       COUNT(*) FROM dbo.Employees WHERE IsTeachingStaff = 1
UNION ALL SELECT 'Non-teaching staff',                   COUNT(*) FROM dbo.Employees WHERE IsTeachingStaff = 0
UNION ALL SELECT 'SMSStaffSubject rows',                 COUNT(*) FROM dbo.SMSStaffSubject
UNION ALL SELECT 'SMSStaffSubject with no employee (0)', COUNT(*) FROM dbo.SMSStaffSubject s
          WHERE NOT EXISTS (SELECT 1 FROM dbo.Employees e WHERE e.Id = s.TeacherID);
GO

PRINT '';
PRINT '=== NEXT: re-run these so their stored procedures read dbo.Employees ===';
PRINT '    SMSStaffSubject.sql';
PRINT '    SpRPT_SMSExamExecutive.sql';
PRINT '    SpRPT_SMSExamSubjectWise.sql';
PRINT '    SpRPT_SMSExamTeacherComparison.sql';
PRINT '    SpRPT_SMSExamTeacherWise.sql';
GO

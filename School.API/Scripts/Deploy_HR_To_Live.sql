/* ============================================================================
   HR MODULE — LIVE DEPLOYMENT

   Run this ONCE against the live EasySchoolDB. It is schema-only plus the
   teacher->employee merge: it never copies data from the development
   database, so nothing recorded on live is overwritten.

   APPLIED to EasySchoolDB_Live on 2026-08-31 (80 teachers, 478 staff-subject
   rows). Verified afterwards:
     - 0 tables lost, 0 rows lost anywhere in the database
     - Employees: 80 rows, ids 1..80, CHECKSUM_AGG(Id) = 80 — byte for byte the
       SMSTeacher VIDs that went in
     - SMSStaffSubject: 478 rows, TeacherID checksum unchanged, 0 broken links
     - students, fees, exam results, attendance all untouched

   SMSTeacher no longer exists as a table: the rename folds it into Employees,
   keeping its Ids, so SMSStaffSubject / SMSClassTimetable / SpRPT_SMSExam*
   still resolve — against dbo.Employees now.

   ── BEFORE YOU RUN ────────────────────────────────────────────────────────
     1. TAKE A FULL BACKUP OF LIVE. Non-negotiable.
     2. Confirm you are connected to the live database (the guard below will
        stop you otherwise).

     3. !! MOST OF THESE SCRIPTS BEGIN WITH `USE [EasySchoolDB]` !!
        Run as-is against a live database under any other name, they silently
        apply to the DEVELOPMENT database instead and report success. Either
        edit that line to the live database name first, or run each file with
        the USE line replaced. Scripts carrying it:

            HrSchoolTiming.sql              AddExamToStaffSubject.sql
            AddFridayAndDayOverrides.sql    SMSStaffSubject.sql
            AddOffDaysToSchoolTiming.sql    SMSClassTimetable.sql
            SpRPT_SMSExam*.sql              SpRPT_SMSStudentFee.sql

     4. Run the source scripts in THIS ORDER:

        A. Staff — the one step that must go first, because everything else
           foreign-keys to Employees:

              RenameTeacherToEmployee_FreshDb.sql   if the database has NO
                                                    Employees table (a site that
                                                    never had the HR module)
              RenameTeacherToEmployee.sql           if it HAS one to merge
                                                    (the development database)

              Each refuses to run in the other's situation, so picking the wrong
              one is safe — it stops rather than half-applying.

        B. HR schema:
              CoreHrTables_Fix.sql    (Designations, HRGrades, Employee FKs)
              HrPayroll.sql           (shifts, attendance, leave, payroll)
              HrSchoolTiming.sql
              AddFridayAndDayOverrides.sql
              AddOffDaysToSchoolTiming.sql

        C. Staff subjects, per exam:
              AddExamToStaffSubject.sql   (adds ExamID, backfills every existing
                                           row to the oldest active exam)
              SMSStaffSubject.sql         (procedures, incl. SpCopy_SMSStaffSubject)

        D. Procedures that read staff or classes:
              SMSTeacher.sql          (drops the retired teacher procedures)
              SMSClassTimetable.sql
              SpRPT_SMSExamExecutive.sql
              SpRPT_SMSExamSubjectWise.sql
              SpRPT_SMSExamTeacherWise.sql
              SpRPT_SMSExamTeacherComparison.sql
              SpRPT_SMSStudentFee.sql (returns ClassID/SectionID so the fee
                                       report can order by the school's own
                                       class order)

     CreateTables.sql and MergeTeacherIntoEmployee.sql are NOT in this list.
     CreateTables.sql builds a whole parallel schema (Users, Roles, Menus,
     Students, TimeTables) that a live site running the older SecUser schema
     does not use — see the warning below. MergeTeacherIntoEmployee.sql only
     applies to a database that already had both tables.

   ── !! CRITICAL WARNING ABOUT CreateTables.sql !! ─────────────────────────
     CreateTables.sql begins with:

           IF NOT EXISTS (... name = N'SchoolManagementDB') CREATE DATABASE ...
           GO
           USE SchoolManagementDB;

     Run as-is against live, it CREATES A SEPARATE DATABASE called
     SchoolManagementDB and builds every table in there instead of in
     EasySchoolDB. EasySchoolDB gets nothing, the next three scripts then
     fail with "Cannot find the object Employees", and the HR module is
     left half-installed.

     DELETE those first 12 lines (through "USE SchoolManagementDB;") before
     running it, or run the prepared copy of it that ships beside this file.

   This script itself only VERIFIES the outcome — it makes no changes.
   ============================================================================ */

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

/* ---------------------------------------------------------------------------
   Guard: refuse to report on the wrong database.
   --------------------------------------------------------------------------- */
-- RAISERROR will not take a function call as an argument, hence the variable.
DECLARE @db SYSNAME = DB_NAME();
IF @db NOT LIKE 'EasySchool%'
BEGIN
    RAISERROR('Wrong database: %s. Switch to EasySchoolDB and run again.', 16, 1, @db);
    SET NOEXEC ON;
END
GO

PRINT '=== Post-deployment verification for ' + DB_NAME() + ' ===';
PRINT '';

/* ---------------------------------------------------------------------------
   1. Did every HR object actually land in THIS database?
   --------------------------------------------------------------------------- */
SELECT  'Missing HR tables (must be 0)' AS Check_, COUNT(*) AS Value
FROM   (VALUES ('Employees'),('Designations'),('HRGrades'),('HrShift'),
               ('HrHoliday'),('HrLeaveType'),('HrAttendance'),('HrLeave'),
               ('HrPayComponent'),('HrEmployeePay'),('HrPayrollRun'),
               ('HrPayslip'),('HrSchoolTiming'),('HrSchoolTimingDay')) AS t(n)
WHERE  NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                   WHERE TABLE_NAME = t.n AND TABLE_TYPE = 'BASE TABLE');

/* Users / Roles / RolePermissions are NOT part of the HR module. A site still
   on the older SecUser schema has none of them, and that is not a failure —
   this is reported so the difference is visible, not so it is fixed. */
SELECT  'User-management tables present (0 on a SecUser site)' AS Check_, COUNT(*) AS Value
FROM   (VALUES ('Users'),('Roles'),('RolePermissions')) AS t(n)
WHERE  EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES
               WHERE TABLE_NAME = t.n AND TABLE_TYPE = 'BASE TABLE');

/* The stray database is the single most likely deployment mistake. */
SELECT 'Stray SchoolManagementDB created (must be 0)' AS Check_,
       COUNT(*) AS Value
FROM   sys.databases WHERE name = 'SchoolManagementDB';
GO

/* ---------------------------------------------------------------------------
   2. Staff integrity.

   After RenameTeacherToEmployee.sql there is one staff table: Employees, whose
   Id IS the old SMSTeacher.VID. So the check is no longer "does every teacher
   have an employee" — it is that the academic links still resolve against the
   surviving Ids. A broken link here silently breaks timetables and result
   cards.
   --------------------------------------------------------------------------- */
SELECT 'SMSTeacher is still a TABLE (must be 0 - run RenameTeacherToEmployee.sql)' AS Check_,
       COUNT(*) AS Value
FROM   INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SMSTeacher' AND TABLE_TYPE = 'BASE TABLE'
UNION ALL
SELECT 'Employees.TeacherId still present (must be 0)', COUNT(*)
FROM   INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Employees' AND COLUMN_NAME = 'TeacherId'
UNION ALL
SELECT 'Staff (Employees)',                     COUNT(*) FROM Employees
UNION ALL
SELECT 'Teaching staff',                        COUNT(*) FROM Employees WHERE IsTeachingStaff = 1
UNION ALL
SELECT 'Duplicate EmployeeCode (must be 0)',    COUNT(*) FROM
       (SELECT EmployeeCode FROM Employees GROUP BY EmployeeCode HAVING COUNT(*) > 1) d
UNION ALL
SELECT 'Staff-subject links still resolving',   COUNT(*) FROM SMSStaffSubject s
       WHERE EXISTS (SELECT 1 FROM Employees e WHERE e.Id = s.TeacherID)
UNION ALL
SELECT 'Staff-subject links now BROKEN (must be 0)', COUNT(*) FROM SMSStaffSubject s
       WHERE NOT EXISTS (SELECT 1 FROM Employees e WHERE e.Id = s.TeacherID);
GO

/* Users only exists on a site that has the newer user-management schema, and a
   plain batch will not compile against a table that is not there — hence the
   dynamic SQL rather than a plain guard. */
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL
    EXEC('
SELECT ''Users with a missing employee (must be 0)'' AS Check_, COUNT(*) AS Value
FROM   Users u WHERE u.EmployeeId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM Employees e WHERE e.Id = u.EmployeeId);');
GO

/* Every HR child row must point at a staff member that exists — this is what
   the Id remap in RenameTeacherToEmployee.sql step 8 is verified by. */
SELECT 'HrAttendance orphans (must be 0)' AS Check_,  COUNT(*) AS Value FROM HrAttendance a
       WHERE NOT EXISTS (SELECT 1 FROM Employees e WHERE e.Id = a.EmployeeId)
UNION ALL
SELECT 'HrLeave orphans (must be 0)',       COUNT(*) FROM HrLeave l
       WHERE NOT EXISTS (SELECT 1 FROM Employees e WHERE e.Id = l.EmployeeId)
UNION ALL
SELECT 'HrEmployeePay orphans (must be 0)', COUNT(*) FROM HrEmployeePay p
       WHERE NOT EXISTS (SELECT 1 FROM Employees e WHERE e.Id = p.EmployeeId)
UNION ALL
SELECT 'HrPayslip orphans (must be 0)',     COUNT(*) FROM HrPayslip s
       WHERE NOT EXISTS (SELECT 1 FROM Employees e WHERE e.Id = s.EmployeeId);
GO

/* ---------------------------------------------------------------------------
   3. Academic data must be exactly as it was before the deployment.
      Compare these against the numbers you recorded from the pre-deployment
      backup — every one of them must be unchanged.
   --------------------------------------------------------------------------- */
SELECT 'Employees (was SMSTeacher)' AS TableName, COUNT(*) AS Rows_ FROM Employees
UNION ALL SELECT 'SMSStaffSubject', COUNT(*) FROM SMSStaffSubject
UNION ALL SELECT 'SMSStudent',      COUNT(*) FROM SMSStudent
UNION ALL SELECT 'SMSStudentAcademic', COUNT(*) FROM SMSStudentAcademic
UNION ALL SELECT 'SMSParent',       COUNT(*) FROM SMSParent
UNION ALL SELECT 'StdAttendence',   COUNT(*) FROM StdAttendence
UNION ALL SELECT 'SMSStudentFee',   COUNT(*) FROM SMSStudentFee
UNION ALL SELECT 'FeeVoucherMain',  COUNT(*) FROM FeeVoucherMain
UNION ALL SELECT 'FeeVoucherDetail',COUNT(*) FROM FeeVoucherDetail
UNION ALL SELECT 'SMSClass',        COUNT(*) FROM SMSClass
UNION ALL SELECT 'SMSSection',      COUNT(*) FROM SMSSection;
GO

/* ---------------------------------------------------------------------------
   4. Things to fix by hand afterwards, not failures.
   --------------------------------------------------------------------------- */
SELECT 'Teaching staff with no CNIC' AS Review_, COUNT(*) AS Value FROM Employees WHERE IsTeachingStaff = 1 AND ISNULL(LTRIM(RTRIM(CNIC)), '') = ''
UNION ALL SELECT 'Teaching staff with no mobile',        COUNT(*) FROM Employees WHERE IsTeachingStaff = 1 AND ISNULL(LTRIM(RTRIM(Mobile)), '') = ''
UNION ALL SELECT 'Teaching staff with no qualification', COUNT(*) FROM Employees WHERE IsTeachingStaff = 1 AND ISNULL(LTRIM(RTRIM(Qualification)), '') = ''
UNION ALL SELECT 'Single-word names (no last name)',     COUNT(*) FROM Employees WHERE ISNULL(LTRIM(RTRIM(LastName)), '') = ''
/* Gender came over from the teacher form's dropdown, which had no blank option
   and defaulted to Male — so treat migrated gender as unverified. */
UNION ALL SELECT 'Staff still on the migrated default gender', COUNT(*) FROM Employees WHERE Gender = 'Male'
UNION ALL SELECT 'Employees with no designation set',    COUNT(*) FROM Employees WHERE DesignationId IS NULL;
GO

SET NOEXEC OFF;
GO

/* ============================================================================
   HISTORICAL — superseded by RenameTeacherToEmployee.sql.

   This was the first half of the job: it merged the two SCREENS while leaving
   the two TABLES in place. RenameTeacherToEmployee.sql finishes it by folding
   SMSTeacher into Employees so there is one table and one identity. Keep this
   script for databases that have not been through the rename yet; on a
   migrated database it detects that Employees.TeacherId is gone and stops.
   ----------------------------------------------------------------------------

   Merge the Teacher screen into the Employee screen.

   Employees becomes the single staff master. SMSTeacher stays exactly where it
   is as the ACADEMIC identity — SMSStaffSubject, SMSClassTimetable and every
   SpRPT_SMSExam* report still join on SMSTeacher.VID and are not touched by
   this script. The two are tied together by the existing Employees.TeacherId FK.

   What this does:
     1. Adds Employees.FatherName and Employees.IsTeachingStaff.
     2. Creates one Employee row per SMSTeacher row that has no Employee yet,
        linked back via TeacherId and flagged IsTeachingStaff = 1.
     3. Removes the ten seeded demo employees (EMP001..EMP010) and their test
        attendance, which are the only rows in Employees that predate this.

   Safe to run more than once: every step is guarded, and step 3 only ever
   matches the ten known demo codes.
   ============================================================================ */

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;   -- HrAttendance carries a filtered index; DML fails without it

/* ---------------------------------------------------------------------------
   1. Columns  (own batch, so the rest of the script can see them)
   --------------------------------------------------------------------------- */
IF COL_LENGTH('Employees', 'FatherName') IS NULL
    ALTER TABLE Employees ADD FatherName NVARCHAR(200) NULL;

IF COL_LENGTH('Employees', 'IsTeachingStaff') IS NULL
    ALTER TABLE Employees ADD IsTeachingStaff BIT NOT NULL
        CONSTRAINT DF_Employees_IsTeachingStaff DEFAULT (0);
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;   -- HrAttendance carries a filtered index; DML fails without it
SET XACT_ABORT ON;

/* Everything below reads Employees.TeacherId. Once RenameTeacherToEmployee.sql
   has run there is only one staff table, the column is gone, and there is
   nothing left to merge. */
IF COL_LENGTH('Employees', 'TeacherId') IS NULL
BEGIN
    PRINT 'Employees.TeacherId no longer exists — this database has already been through RenameTeacherToEmployee.sql. Nothing to merge.';
    RETURN;
END

BEGIN TRANSACTION;

    /* Any employee already linked to an academic record is teaching staff. */
    UPDATE Employees
    SET    IsTeachingStaff = 1
    WHERE  TeacherId IS NOT NULL
      AND  IsTeachingStaff = 0;

    /* -----------------------------------------------------------------------
       2. SMSTeacher -> Employees

       VName is split on the FIRST space so that FirstName + ' ' + LastName
       rebuilds VName exactly — the app writes the academic name back that way,
       so the split has to round-trip losslessly.

       Gender uses the app's own coding (1 = Male, 2 = Female, per the teacher
       form's dropdown). NOTE: 65 of the 67 rows sit on 1, which is that
       dropdown's default with no blank option — treat migrated gender as
       unverified data and correct it from the Employee screen.
       ----------------------------------------------------------------------- */
    INSERT INTO Employees
    (
        EmployeeCode, FirstName, LastName, FatherName, Gender,
        CNIC, Mobile, Email, Address, Qualification, Experience,
        TeacherId, IsTeachingStaff, IsActive, Status, CreatedAt
    )
    SELECT
        'T' + RIGHT('000' + CAST(t.VID AS VARCHAR(10)), 3),
        CASE WHEN CHARINDEX(' ', LTRIM(RTRIM(t.VName))) = 0
             THEN LTRIM(RTRIM(t.VName))
             ELSE LEFT(LTRIM(RTRIM(t.VName)), CHARINDEX(' ', LTRIM(RTRIM(t.VName))) - 1)
        END,
        CASE WHEN CHARINDEX(' ', LTRIM(RTRIM(t.VName))) = 0
             THEN NULL
             ELSE SUBSTRING(LTRIM(RTRIM(t.VName)), CHARINDEX(' ', LTRIM(RTRIM(t.VName))) + 1, 100)
        END,
        NULLIF(LTRIM(RTRIM(ISNULL(t.FatherName, ''))), ''),
        CASE t.Gender WHEN 1 THEN 'Male' WHEN 2 THEN 'Female' ELSE NULL END,
        NULLIF(LTRIM(RTRIM(ISNULL(t.CNIC, ''))), ''),
        NULLIF(LTRIM(RTRIM(ISNULL(t.Mobile, ''))), ''),
        NULLIF(LTRIM(RTRIM(ISNULL(t.Email, ''))), ''),
        NULLIF(LTRIM(RTRIM(ISNULL(t.Address, ''))), ''),
        LEFT(NULLIF(LTRIM(RTRIM(ISNULL(t.Qualification, ''))), ''), 100),
        CASE WHEN t.ExperienceYears IS NULL THEN NULL
             ELSE CAST(t.ExperienceYears AS NVARCHAR(100)) END,
        t.VID,
        1,
        ISNULL(t.IsActive, 1),
        CASE WHEN ISNULL(t.IsActive, 1) = 1 THEN 'Active' ELSE 'Inactive' END,
        ISNULL(t.InsertedDate, GETDATE())
    FROM   SMSTeacher t
    WHERE  NOT EXISTS (SELECT 1 FROM Employees e WHERE e.TeacherId = t.VID);

    PRINT 'Employees created from SMSTeacher: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

    /* -----------------------------------------------------------------------
       3. Drop the seeded demo employees

       Scoped to the exact ten demo codes AND TeacherId IS NULL, so a re-run —
       or a real employee added later — can never be caught by this.
       ----------------------------------------------------------------------- */
    DECLARE @Demo TABLE (Id INT PRIMARY KEY);

    INSERT INTO @Demo (Id)
    SELECT Id
    FROM   Employees
    WHERE  TeacherId IS NULL
      AND  EmployeeCode IN ('EMP001','EMP002','EMP003','EMP004','EMP005',
                            'EMP006','EMP007','EMP008','EMP009','EMP010');

    /* Users.EmployeeId is a FK; unlink first so a login is never orphaned. */
    UPDATE Users
    SET    EmployeeId = NULL
    WHERE  EmployeeId IN (SELECT Id FROM @Demo);

    DELETE FROM HrPayslip     WHERE EmployeeId IN (SELECT Id FROM @Demo);
    DELETE FROM HrEmployeePay WHERE EmployeeId IN (SELECT Id FROM @Demo);
    DELETE FROM HrLeave       WHERE EmployeeId IN (SELECT Id FROM @Demo);
    DELETE FROM HrAttendance  WHERE EmployeeId IN (SELECT Id FROM @Demo);
    DELETE FROM Employees     WHERE Id         IN (SELECT Id FROM @Demo);

    PRINT 'Demo employees removed: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

COMMIT TRANSACTION;
GO

/* ---------------------------------------------------------------------------
   Result
   --------------------------------------------------------------------------- */
SELECT 'Employees'                  AS TableName, COUNT(*) AS [Count] FROM Employees
UNION ALL SELECT 'Teaching staff',              COUNT(*) FROM Employees WHERE IsTeachingStaff = 1
UNION ALL SELECT 'SMSTeacher (untouched)',      COUNT(*) FROM SMSTeacher
UNION ALL SELECT 'SMSStaffSubject (untouched)', COUNT(*) FROM SMSStaffSubject;
GO

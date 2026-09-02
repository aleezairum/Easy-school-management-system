/* ============================================================================
   SMSTeacher — RETIRED.

   The table was folded into dbo.Employees by RenameTeacherToEmployee.sql:
   Employees.Id IS the old SMSTeacher.VID, so SMSStaffSubject.TeacherID,
   SMSClassTimetable.TeacherID and every SpRPT_SMSExam* report still resolve —
   they just join dbo.Employees now.

   Its three stored procedures are gone with it. There is no separate teacher
   record left to insert, update or delete: staff are created and edited on the
   Employee screen (EF Core, EmployeeRepository), and ticking "Teaching staff"
   is what makes someone available to subject assignment and the exam reports.

   Running this script drops the procedures if they are still around. The read
   -only compatibility VIEW named dbo.SMSTeacher that the rename script leaves
   behind is NOT dropped here — do that by hand once you have confirmed nothing
   in the live database still selects from it:

       DROP VIEW dbo.SMSTeacher;

   Target: SQL Server 2019
   ============================================================================ */

SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.SpSave_SMSTeacher', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.SpSave_SMSTeacher;
    PRINT '>> SpSave_SMSTeacher dropped.';
END
GO

IF OBJECT_ID('dbo.SpDelete_SMSTeacher', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.SpDelete_SMSTeacher;
    PRINT '>> SpDelete_SMSTeacher dropped.';
END
GO

IF OBJECT_ID('dbo.SpGet_SMSTeacher', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.SpGet_SMSTeacher;
    PRINT '>> SpGet_SMSTeacher dropped.';
END
GO

IF OBJECT_ID('dbo.SMSTeacher', 'U') IS NOT NULL
    PRINT '!! dbo.SMSTeacher is still a TABLE — run RenameTeacherToEmployee.sql first.';
ELSE
    PRINT '>> dbo.SMSTeacher is no longer a table. Staff live in dbo.Employees.';
GO

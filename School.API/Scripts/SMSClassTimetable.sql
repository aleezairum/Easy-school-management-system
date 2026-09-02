/* ============================================================================
   SMSClassTimetable — stored procedure

   This procedure lived only in the live database; it was found still naming
   SMSTeacher after RenameTeacherToEmployee.sql and is captured here so it is
   version-controlled like the rest.

   The only change from what was in the database: the staff join now reads
   dbo.Employees. TeacherID values are untouched — Employees.Id IS the old
   SMSTeacher.VID — and VName is still there as a computed column, so the
   result set is identical. Repointing it is what lets the temporary
   dbo.SMSTeacher compatibility view be dropped.

   Target: SQL Server 2019
   ============================================================================ */

USE [EasySchoolDB]
GO

IF OBJECT_ID('dbo.SpGet_SMSClassTimetable', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SpGet_SMSClassTimetable;
GO

CREATE PROCEDURE dbo.SpGet_SMSClassTimetable
    @VID INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        T.VID,
        T.ClassID, C.VName AS ClassName,
        T.SectionID, S.VName AS SectionName,
        T.SubjectID, Subj.VName AS SubjectName,
        T.TeacherID, Teach.VName AS TeacherName,
        T.DayOfWeek,
        T.TimeFrom,
        T.TimeTo,
        T.IsActive,
        T.InsertedBy, T.InsertedDate, T.InsertedIp,
        T.UpdatedBy, T.UpdatedDate, T.UpdatedIp
    FROM        dbo.SMSClassTimetable T
    INNER JOIN  dbo.SMSClass   C    ON T.ClassID   = C.VID
    INNER JOIN  dbo.SMSSection S    ON T.SectionID = S.VID
    INNER JOIN  dbo.SMSSubject Subj ON T.SubjectID = Subj.VID
    INNER JOIN  dbo.Employees  Teach ON T.TeacherID = Teach.Id
    WHERE       (@VID = 0 OR T.VID = @VID);
END
GO

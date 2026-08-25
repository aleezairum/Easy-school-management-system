USE [EasySchoolDB]
GO
/* ============================================================================
   Adds Result-Card (Rcard) attachment context columns to SmsMessages.
   For MessageType = Rcard (9) the SMS queue processor regenerates the
   student's result card PDF at send time from these references and delivers
   it as a WhatsApp document. All columns are nullable (normal text messages
   leave them NULL). Idempotent — safe to re-run.
   ============================================================================ */
IF COL_LENGTH('dbo.SmsMessages', 'RefExamId') IS NULL
    ALTER TABLE dbo.SmsMessages ADD RefExamId INT NULL;
GO
IF COL_LENGTH('dbo.SmsMessages', 'RefStudentId') IS NULL
    ALTER TABLE dbo.SmsMessages ADD RefStudentId INT NULL;
GO
IF COL_LENGTH('dbo.SmsMessages', 'RefClassId') IS NULL
    ALTER TABLE dbo.SmsMessages ADD RefClassId INT NULL;
GO
IF COL_LENGTH('dbo.SmsMessages', 'RefSectionId') IS NULL
    ALTER TABLE dbo.SmsMessages ADD RefSectionId INT NULL;
GO
IF COL_LENGTH('dbo.SmsMessages', 'RefExamName') IS NULL
    ALTER TABLE dbo.SmsMessages ADD RefExamName NVARCHAR(200) NULL;
GO

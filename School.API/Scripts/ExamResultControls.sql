/* ============================================================================
   Exam result controls
   ----------------------------------------------------------------------------
   Run on the target database:
     sqlcmd -S . -U sa -P <pwd> -d EasySchoolDB -i ExamResultControls.sql

   SMSExamExclude    students left out of the result of one exam
                     (Exam -> Result Exclude List). Their marks stay as they
                     are; every SpRPT_SMSExam* result report, the mobile app
                     and the WhatsApp result skip them. Run those report
                     scripts after this one.

   SMSExamFinalize   one row per exam whose result the office has finalised
                     (Exams screen -> Finalize). WhatsApp shows parents and
                     teachers only finalised exams, so marks half entered do
                     not go out. No row = not finalised.

   New tables only. Nothing existing is changed.
   ============================================================================ */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.SMSExamExclude', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SMSExamExclude
    (
        VID           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SMSExamExclude PRIMARY KEY,
        ExamID        INT           NOT NULL,
        StudentID     INT           NOT NULL,
        Reason        NVARCHAR(200) NULL,
        InsertedBy    INT           NULL,
        InsertedDate  DATETIME      NOT NULL CONSTRAINT DF_SMSExamExclude_Date DEFAULT GETDATE(),
        CONSTRAINT UX_SMSExamExclude_Exam_Student UNIQUE (ExamID, StudentID)
    );
END
GO

IF OBJECT_ID('dbo.SMSExamFinalize', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SMSExamFinalize
    (
        ExamID         INT      NOT NULL CONSTRAINT PK_SMSExamFinalize PRIMARY KEY,
        FinalizedBy    INT      NULL,
        FinalizedDate  DATETIME NOT NULL CONSTRAINT DF_SMSExamFinalize_Date DEFAULT GETDATE()
    );
END
GO

SELECT 'Result exclude list : ' + CASE WHEN OBJECT_ID('dbo.SMSExamExclude') IS NULL THEN 'missing' ELSE 'ready' END;
SELECT 'Result finalize     : ' + CASE WHEN OBJECT_ID('dbo.SMSExamFinalize') IS NULL THEN 'missing' ELSE 'ready' END;

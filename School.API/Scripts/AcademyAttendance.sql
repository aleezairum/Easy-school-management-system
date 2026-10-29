/* ============================================================================
   Academy attendance
   ----------------------------------------------------------------------------
   Run on the target database:
     sqlcmd -S . -U sa -P <pwd> -d EasySchoolDB -i AcademyAttendance.sql

   SMSAcademyAttendance   one row per student per day of the academy (the
                          after-school classes): 1 Present, 0 Absent, 2 Leave.
                          Students marked here are the ones with "Avail Academy"
                          on (SMSStudentAcademic.IsAvailAcademy).

   Kept apart from StdAttendence on purpose: the absent SMS, the dashboard,
   the monthly register, WhatsApp, the mobile app and the face machines all
   read StdAttendence, and none of them should count an academy day.

   New table only. Nothing existing is changed. Safe to run more than once.
   ============================================================================ */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.SMSAcademyAttendance', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SMSAcademyAttendance
    (
        VID           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SMSAcademyAttendance PRIMARY KEY,
        StudentID     INT           NOT NULL,
        SessionID     INT           NULL,
        SectionID     INT           NULL,
        VDate         DATE          NOT NULL,
        Attendence    TINYINT       NOT NULL,          -- 1 Present, 0 Absent, 2 Leave
        Remarks       NVARCHAR(200) NULL,
        InsertedBy    INT           NULL,
        InsertedDate  DATETIME      NOT NULL CONSTRAINT DF_SMSAcademyAttendance_Date DEFAULT GETDATE(),
        UpdatedBy     INT           NULL,
        UpdatedDate   DATETIME      NULL,
        CONSTRAINT UX_SMSAcademyAttendance_Student_Date UNIQUE (StudentID, VDate),
        CONSTRAINT CK_SMSAcademyAttendance_Value CHECK (Attendence IN (0, 1, 2))
    );
    CREATE INDEX IX_SMSAcademyAttendance_Date ON dbo.SMSAcademyAttendance (VDate, SectionID);
END
GO

SELECT 'Academy attendance : ' + CASE WHEN OBJECT_ID('dbo.SMSAcademyAttendance') IS NULL THEN 'missing' ELSE 'ready' END;
GO

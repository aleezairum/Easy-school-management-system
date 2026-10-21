/* ============================================================================
   Attendance machines: faces and fingerprints kept in EasySchool.
   - HrBiometricFinger: each person's fingerprint templates, as read from a
     machine, so they can be put on another machine (same finger works there).
   - HrBiometricFace.SourceDeviceId: the machine a face was read from (NULL =
     uploaded in EasySchool).
   - HrDevicePerson.FingersOnDevice: how many fingerprints the machine holds.
   - Employees.Photo widened to NVARCHAR(MAX): the employee form saves the
     picture itself (a data: URL), which never fitted in 255 characters.
   Run after HikvisionAttendance.sql. Safe to run again.
   ============================================================================ */
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.HrBiometricFinger', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HrBiometricFinger
    (
        PersonType      TINYINT        NOT NULL,   -- 1 staff, 2 student
        PersonId        INT            NOT NULL,
        FingerNo        TINYINT        NOT NULL,   -- the machine's fingerPrintID, 1..10
        FingerType      NVARCHAR(20)   NOT NULL CONSTRAINT DF_HrBFi_Type DEFAULT ('normalFP'),
        TemplateData    NVARCHAR(MAX)  NOT NULL,   -- base64, exactly as the machine gave it
        SourceDeviceId  INT            NULL,
        UpdatedAt       DATETIME       NOT NULL CONSTRAINT DF_HrBFi_Updated DEFAULT (GETDATE()),
        CONSTRAINT PK_HrBiometricFinger PRIMARY KEY (PersonType, PersonId, FingerNo)
    );
END
GO

IF COL_LENGTH('dbo.HrBiometricFace', 'SourceDeviceId') IS NULL
    ALTER TABLE dbo.HrBiometricFace ADD SourceDeviceId INT NULL;
GO

IF COL_LENGTH('dbo.HrDevicePerson', 'FingersOnDevice') IS NULL
    ALTER TABLE dbo.HrDevicePerson ADD FingersOnDevice TINYINT NULL;
GO

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
           WHERE TABLE_NAME = 'Employees' AND COLUMN_NAME = 'Photo' AND CHARACTER_MAXIMUM_LENGTH <> -1)
    ALTER TABLE dbo.Employees ALTER COLUMN Photo NVARCHAR(MAX) NULL;
GO

SELECT 'Faces and fingerprints : ' + CASE WHEN OBJECT_ID('dbo.HrBiometricFinger') IS NULL THEN 'missing' ELSE 'ready' END;
GO

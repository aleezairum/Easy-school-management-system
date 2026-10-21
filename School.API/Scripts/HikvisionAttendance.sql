/* ============================================================================
   Hikvision attendance machines - staff and students marked by face
   ============================================================================

   Deliberately NO "USE [EasySchoolDB]" here: a hard-coded USE overrides
   sqlcmd's -d. The database comes from -d instead.

     cd School.API\Scripts
     sqlcmd -S . -U sa -P <pwd> -d EasySchoolDB -i HikvisionAttendance.sql

   WHAT THIS CREATES - all new; nothing existing is changed

     HrAttendanceDevice   the machines: campus, address, login, and the key in
                          the address a machine pushes its punches to.
     HrDevicePerson       who has been sent to which machine, and whether their
                          face and card went with them.
     HrBiometricFace      a face photo uploaded from EasySchool, kept so a
                          replaced or reset machine can be filled again.
     HrDeviceEvent        every punch received, pushed or pulled, with what was
                          done with it - the log the screen shows.

   On a machine a person is "E<employee id>" (staff) or "S<student id>"
   (students). Staff punches become the day's time in and time out in staff
   attendance; a student punch marks the student Present for the day, and only
   when the class teacher has not already marked them.

   Additive and safe to re-run.
   ============================================================================ */
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('dbo.HrAttendanceDevice', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HrAttendanceDevice
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HrAttendanceDevice PRIMARY KEY,
        Name            NVARCHAR(100)  NOT NULL,
        CampusId        INT            NULL,
        IpAddress       NVARCHAR(100)  NOT NULL,
        Port            INT            NOT NULL CONSTRAINT DF_HrAD_Port DEFAULT (80),
        UseHttps        BIT            NOT NULL CONSTRAINT DF_HrAD_Https DEFAULT (0),
        Username        NVARCHAR(50)   NOT NULL CONSTRAINT DF_HrAD_User DEFAULT ('admin'),
        Password        NVARCHAR(100)  NULL,
        -- Part of the push address; a push without the right key is refused.
        PushKey         NVARCHAR(40)   NOT NULL,
        IsActive        BIT            NOT NULL CONSTRAINT DF_HrAD_Active DEFAULT (1),
        PullEnabled     BIT            NOT NULL CONSTRAINT DF_HrAD_Pull DEFAULT (1),
        DeviceModel     NVARCHAR(100)  NULL,
        SerialNumber    NVARCHAR(100)  NULL,
        Firmware        NVARCHAR(100)  NULL,
        LastSeenAt      DATETIME       NULL,
        LastPullAt      DATETIME       NULL,
        -- The newest punch read from the machine; the next pull starts a little before it.
        LastEventTime   DATETIME       NULL,
        LastError       NVARCHAR(500)  NULL,
        CreatedAt       DATETIME       NOT NULL CONSTRAINT DF_HrAD_Created DEFAULT (GETDATE()),
        UpdatedAt       DATETIME       NULL
    );
    CREATE UNIQUE INDEX UX_HrAD_PushKey ON dbo.HrAttendanceDevice (PushKey);
END
GO

IF OBJECT_ID('dbo.HrDevicePerson', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HrDevicePerson
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HrDevicePerson PRIMARY KEY,
        DeviceId        INT            NOT NULL CONSTRAINT FK_HrDP_Device REFERENCES dbo.HrAttendanceDevice(Id) ON DELETE CASCADE,
        PersonType      TINYINT        NOT NULL,   -- 1 staff, 2 student
        PersonId        INT            NOT NULL,
        EmployeeNo      NVARCHAR(32)   NOT NULL,
        CardNo          NVARCHAR(32)   NULL,
        UserSyncedAt    DATETIME       NULL,
        FaceSyncedAt    DATETIME       NULL,
        LastError       NVARCHAR(500)  NULL,
        UpdatedAt       DATETIME       NULL
    );
    CREATE UNIQUE INDEX UX_HrDP_Person ON dbo.HrDevicePerson (DeviceId, PersonType, PersonId);
END
GO

IF OBJECT_ID('dbo.HrBiometricFace', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HrBiometricFace
    (
        PersonType      TINYINT        NOT NULL,
        PersonId        INT            NOT NULL,
        ImageData       VARBINARY(MAX) NOT NULL,
        UpdatedAt       DATETIME       NOT NULL CONSTRAINT DF_HrBF_Updated DEFAULT (GETDATE()),
        UpdatedBy       INT            NULL,
        CONSTRAINT PK_HrBiometricFace PRIMARY KEY (PersonType, PersonId)
    );
END
GO

IF OBJECT_ID('dbo.HrDeviceEvent', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HrDeviceEvent
    (
        Id              BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HrDeviceEvent PRIMARY KEY,
        DeviceId        INT            NOT NULL,
        -- The machine's own number for the punch; how a pulled punch already
        -- received by push is recognised and not counted twice.
        SerialNo        BIGINT         NULL,
        EmployeeNo      NVARCHAR(32)   NULL,
        PersonName      NVARCHAR(100)  NULL,
        PersonType      TINYINT        NULL,
        PersonId        INT            NULL,
        EventTime       DATETIME       NOT NULL,
        Major           INT            NOT NULL,
        Minor           INT            NOT NULL,
        VerifyMode      NVARCHAR(40)   NULL,
        ReceivedBy      VARCHAR(10)    NOT NULL,   -- push | pull
        Accepted        BIT            NOT NULL CONSTRAINT DF_HrDE_Accepted DEFAULT (0),
        Result          NVARCHAR(300)  NULL,
        CreatedAt       DATETIME       NOT NULL CONSTRAINT DF_HrDE_Created DEFAULT (GETDATE())
    );
    CREATE UNIQUE INDEX UX_HrDE_Serial ON dbo.HrDeviceEvent (DeviceId, SerialNo) WHERE SerialNo IS NOT NULL;
    CREATE INDEX IX_HrDE_Time ON dbo.HrDeviceEvent (EventTime DESC);
    CREATE INDEX IX_HrDE_Person ON dbo.HrDeviceEvent (PersonType, PersonId, EventTime);
END
GO

SELECT 'Attendance machines : ' + CASE WHEN OBJECT_ID('dbo.HrDeviceEvent') IS NULL THEN 'missing' ELSE 'ready' END;
GO

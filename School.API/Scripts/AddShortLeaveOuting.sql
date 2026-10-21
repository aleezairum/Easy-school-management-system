/* ============================================================================
   Short leave — time out of school during the working day
   ============================================================================

   Deliberately NO "USE [EasySchoolDB]" here: a hard-coded USE overrides
   sqlcmd's -d and sends the script to whatever that name resolves to on the
   machine it runs on. The database comes from -d instead.

     cd School.API\Scripts
     sqlcmd -S . -U sa -P <pwd> -d EasySchoolDB -i AddShortLeaveOuting.sql

   The case this answers: a teacher arrives on time and leaves on time, but is
   away for part of the day. Time-in and time-out alone cannot see that — the
   day looks complete — so the hours are neither recorded nor charged.

   Two objects:

   1. HrAttendanceOuting — one row per trip out, because a person can leave
      twice in a day and recording only a single OUT/IN pair would either lose
      the second trip or overstate the first. A row with no InTime means the
      employee has not come back yet; its minutes count as zero until they do.

   2. HrAttendance.OutingMinutes — the day's total, kept on the attendance row
      itself. Payroll reads a month of attendance for every employee at once;
      making it join and re-add the child rows on every payslip would be slower
      and would let the two disagree. It is written only by the code that saves
      an outing, never by hand.

   The day's STATUS is deliberately untouched by this. Someone who came at the
   bell and left at the bell was present; the hours away are charged through
   the payroll component, not by turning their day into a half day.
   ============================================================================ */
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF COL_LENGTH('dbo.HrAttendance', 'OutingMinutes') IS NULL
    ALTER TABLE dbo.HrAttendance
        ADD OutingMinutes INT NOT NULL CONSTRAINT DF_HrAttendance_OutingMinutes DEFAULT (0);
GO

IF OBJECT_ID('dbo.HrAttendanceOuting', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HrAttendanceOuting
    (
        Id           BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HrAttendanceOuting PRIMARY KEY,
        AttendanceId BIGINT        NOT NULL,
        OutTime      TIME(0)       NOT NULL,
        InTime       TIME(0)       NULL,          -- NULL = still out
        Minutes      INT           NOT NULL CONSTRAINT DF_HrAttendanceOuting_Minutes DEFAULT (0),
        Reason       NVARCHAR(300) NULL,
        CreatedBy    INT           NULL,
        CreatedAt    DATETIME      NOT NULL CONSTRAINT DF_HrAttendanceOuting_CreatedAt DEFAULT (GETDATE()),

        -- Deleting the attendance row takes its outings with it: an outing
        -- describes a day that no longer exists.
        CONSTRAINT FK_HrAttendanceOuting_Attendance
            FOREIGN KEY (AttendanceId) REFERENCES dbo.HrAttendance (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_HrAttendanceOuting_Attendance ON dbo.HrAttendanceOuting (AttendanceId);
END
GO

/* A pay component the school configures like any other, so the rate per minute
   is theirs to set — the same way late arrival is already charged.

   Not inserted as a live component: adding it here would start deducting from
   everybody's salary the moment this script runs. It is created INACTIVE, for
   the office to price and switch on from Payroll Setup > Pay Components. */
IF NOT EXISTS (SELECT 1 FROM dbo.HrPayComponent WHERE Code = 'SHORTLEAVE')
BEGIN
    INSERT dbo.HrPayComponent
        (Code, Name, Kind, CalcType, BaseOn, RateSource, Rate, ScopeType, Priority, ShowOnPayslip, EffectiveFrom, IsActive)
    VALUES
        (N'SHORTLEAVE', N'Short Leave (time out of school)',
         2,      -- Kind:       Deduction
         8,      -- CalcType:   PerOutingMinute
         1,      -- BaseOn:     Basic
         2,      -- RateSource: Derived — per-minute rate worked out from the basic
         NULL,   -- Rate:       derived, so no fixed figure
         1,      -- ScopeType:  Global
         50,     -- Priority
         1,      -- ShowOnPayslip — the staff member can see what was charged
         GETDATE(),
         0);     -- IsActive:   OFF until the school prices it and turns it on
END
GO

SELECT 'HrAttendanceOuting     : ' + CASE WHEN OBJECT_ID('dbo.HrAttendanceOuting') IS NULL THEN 'missing' ELSE 'ready' END;
SELECT 'HrAttendance.OutingMinutes : ' + CASE WHEN COL_LENGTH('dbo.HrAttendance','OutingMinutes') IS NULL THEN 'missing' ELSE 'ready' END;
SELECT 'SHORTLEAVE component   : ' + ISNULL((SELECT 'created, IsActive=' + CAST(IsActive AS VARCHAR)
                                             FROM dbo.HrPayComponent WHERE Code = 'SHORTLEAVE'), 'missing');
GO

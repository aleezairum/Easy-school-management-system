/* ============================================================================
   School Timings — one school-wide start/end time, applied over a date range.

   Two tables, because the two things are genuinely different:

     HrSchoolTiming      what was entered: "Winter Timings, 1 Nov - 28 Feb,
                         08:30 - 13:30". This is the row the screen lists and
                         edits, as one unit.

     HrSchoolTimingDay   what it resolved to: one row per actual school day in
                         that range. This is what attendance reads — a single
                         lookup by date, no range scanning — and it is where a
                         one-off change to a single day would live.

   A range is not one flat time. Three things bend it, because real school weeks
   are not uniform:

     OffDay1 / OffDay2              the weekly off, 1 = Sunday .. 7 = Saturday.
                                    Those weekdays get no day row at all.

     FridayTimeIn / FridayTimeOut   optional; when set, Fridays in the range get
                                    these instead of the main times.

     HrSchoolTimingDay.IsOverridden set when one particular day is edited by
                                    hand. Re-saving the range rebuilds every
                                    other day from the header and leaves these
                                    alone — an edit made on purpose should not be
                                    undone by re-entering the range around it.

   Saving a range REPLACES the day rows for every date it covers, so a later,
   narrower range (Ramadan timings inside the winter range) simply wins for its
   own dates. Delete that narrower one and those dates are left with no school
   timing, which means attendance falls back to each employee's own shift.

   Holidays and the weekly off days are both skipped when the range is expanded:
   the school is shut, so there is no timing to apply. Those dates then behave
   like any date no timing covers.

   How attendance uses it (AttendanceService): on a date that HAS a school
   timing, every employee's Time In / Time Out comes from that timing instead of
   their shift's. Grace minutes, half-day and full-day thresholds still come from
   the employee's own shift — the school sets when the day starts and ends, the
   shift sets how lateness is judged. An employee with NO shift runs on the
   school timing alone (grace 0, day-length thresholds derived from its span),
   so a school on one bell needs no per-employee shifts at all.

   Only NEW attendance saves are affected. Rows already saved keep the times they
   were computed with, exactly as when a shift is edited — re-save the month to
   recalculate.

   Idempotent: safe to re-run.
   Target: SQL Server 2019
   ============================================================================ */

USE [EasySchoolDB]
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

------------------------------------------------------------------ Header
IF OBJECT_ID('HrSchoolTiming','U') IS NULL
CREATE TABLE HrSchoolTiming (
    Id         INT IDENTITY(1,1) PRIMARY KEY,
    Name       NVARCHAR(100) NULL,          -- optional label, e.g. "Winter Timings"
    StartDate  DATE          NOT NULL,
    EndDate    DATE          NOT NULL,
    TimeIn        TIME(0)    NOT NULL,      -- school start time
    TimeOut       TIME(0)    NOT NULL,      -- school end time
    FridayTimeIn  TIME(0)    NULL,          -- optional: Friday runs short
    FridayTimeOut TIME(0)    NULL,
    OffDay1       TINYINT    NULL,          -- weekly off, 1=Sun .. 7=Sat
    OffDay2       TINYINT    NULL,
    IsActive   BIT           NOT NULL CONSTRAINT DF_HrSchTm_Act DEFAULT 1,
    CreatedAt  DATETIME2(0)  NOT NULL CONSTRAINT DF_HrSchTm_CA  DEFAULT SYSUTCDATETIME(),
    CreatedBy  INT           NULL,
    UpdatedAt  DATETIME2(0)  NULL,
    UpdatedBy  INT           NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_HrSchTm_Dates')
    ALTER TABLE HrSchoolTiming ADD CONSTRAINT CK_HrSchTm_Dates CHECK (EndDate >= StartDate);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_HrSchTm_Times')
    ALTER TABLE HrSchoolTiming ADD CONSTRAINT CK_HrSchTm_Times CHECK (TimeOut > TimeIn);
GO

------------------------------------------------------------------ Applied days
IF OBJECT_ID('HrSchoolTimingDay','U') IS NULL
CREATE TABLE HrSchoolTimingDay (
    Id       INT IDENTITY(1,1) PRIMARY KEY,
    TimingId INT      NOT NULL,
    TDate        DATE     NOT NULL,
    TimeIn       TIME(0)  NOT NULL,
    TimeOut      TIME(0)  NOT NULL,
    IsOverridden BIT      NOT NULL CONSTRAINT DF_HrSchTmDay_Ovr DEFAULT (0),
    CONSTRAINT FK_HrSchTmDay_Timing FOREIGN KEY (TimingId)
        REFERENCES HrSchoolTiming(Id) ON DELETE CASCADE
);
GO

/* One school timing per calendar day — the whole point of expanding the range
   is that a date resolves to exactly one start/end time. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_HrSchTmDay_Date')
CREATE UNIQUE INDEX UX_HrSchTmDay_Date ON HrSchoolTimingDay(TDate);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_HrSchTmDay_Timing')
CREATE INDEX IX_HrSchTmDay_Timing ON HrSchoolTimingDay(TimingId);
GO


/* ---------------------------------------------------------------------------
   Result
   --------------------------------------------------------------------------- */
SELECT  T.Id, T.Name, T.StartDate, T.EndDate, T.TimeIn, T.TimeOut, T.IsActive,
        COUNT(D.Id) AS DaysApplied
FROM    HrSchoolTiming T
LEFT JOIN HrSchoolTimingDay D ON D.TimingId = T.Id
GROUP BY T.Id, T.Name, T.StartDate, T.EndDate, T.TimeIn, T.TimeOut, T.IsActive
ORDER BY T.StartDate DESC;
GO

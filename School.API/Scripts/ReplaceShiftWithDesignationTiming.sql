/* ============================================================================
   Retire HrShift; make HrSchoolTiming designation-aware
   ============================================================================

   WHY
   ---
   A shift is a fixed pair of times. A school day is not: Friday runs short, and
   any single day can move. HrSchoolTiming already solved that — a date range,
   Friday times, and per-day overrides, expanded into HrSchoolTimingDay.

   What was left over is that the day is not the same length for everyone: a
   Principal, a Head Teacher and a Teacher keep different hours. That is what
   DesignationId adds here. NULL means "every designation" — the school-wide
   default — and a row naming a designation overrides it for that designation
   only. So the common case stays one row, and the exceptions are the ones that
   need entering.

   With that, HrShift has nothing left to say and is removed.

   WHAT MOVES ACROSS
   -----------------
   HrShift did not only carry times; it also carried how the day is JUDGED:

       GraceMinutes     how late is late
       HalfDayMinutes   worked minutes that still count as a half day
       FullDayMinutes   worked minutes that count as a full day
       WorkHours        hours per day, which payroll divides by for
                        per-hour components (HrPayComponent.DivisorHours)

   Dropping the table without these would quietly change every attendance
   calculation and every hourly pay component, so they are added to
   HrSchoolTiming and the existing values are copied over below.

   HalfDayMinutes, FullDayMinutes and WorkHours are NULLABLE on purpose: left
   empty they are derived from the day's own span (37.5% / 75%, the same
   proportions the Shift form defaulted to), so a school running one bell for
   everyone never has to fill them in.

   SAFETY
   ------
   HrShift rows are copied to HrShift_Backup_2026_09 before the table is
   dropped. Employees.ShiftId and HrAttendance.ShiftId are dropped too — check
   the backup table if anything needs to be recovered.

   Idempotent: safe to run more than once.

   Run with:
     cd School.API\Scripts
     sqlcmd -S . -U sa -P <pwd> -d EasySchoolDB -i ReplaceShiftWithDesignationTiming.sql
   ============================================================================ */

-- Filtered indexes are created below, and SQL Server refuses to create one
-- unless QUOTED_IDENTIFIER is ON. sqlcmd leaves it OFF by default, so the
-- script sets it here rather than depending on the caller passing -I.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 1. New columns on HrSchoolTiming --------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('HrSchoolTiming') AND name = 'DesignationId')
BEGIN
    ALTER TABLE HrSchoolTiming ADD DesignationId INT NULL;
    PRINT 'HrSchoolTiming.DesignationId added.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('HrSchoolTiming') AND name = 'GraceMinutes')
BEGIN
    ALTER TABLE HrSchoolTiming ADD
        GraceMinutes   INT NOT NULL CONSTRAINT DF_HrSchTm_Grace DEFAULT 0,
        HalfDayMinutes INT NULL,
        FullDayMinutes INT NULL,
        WorkHours      DECIMAL(5,2) NULL;
    PRINT 'HrSchoolTiming: grace / half-day / full-day / work-hours added.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_HrSchTm_Designation')
   AND EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Designations')
BEGIN
    ALTER TABLE HrSchoolTiming
        ADD CONSTRAINT FK_HrSchTm_Designation
        FOREIGN KEY (DesignationId) REFERENCES Designations(Id);
    PRINT 'FK_HrSchTm_Designation added.';
END
GO

-- 2. Carry the shift settings across ------------------------------------------
-- Only where nothing has been entered yet, so re-running never overwrites what
-- the office has since typed in.
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HrShift')
BEGIN
    DECLARE @Grace INT, @Half INT, @Full INT, @Hours DECIMAL(5,2);

    SELECT TOP 1 @Grace = GraceMinutes, @Half = HalfDayMinutes,
                 @Full = FullDayMinutes, @Hours = WorkHours
    FROM   HrShift
    WHERE  ISNULL(IsActive, 1) = 1
    ORDER BY Id;

    IF @Grace IS NOT NULL
    BEGIN
        UPDATE HrSchoolTiming
        SET GraceMinutes   = CASE WHEN GraceMinutes = 0 THEN @Grace ELSE GraceMinutes END,
            HalfDayMinutes = ISNULL(HalfDayMinutes, @Half),
            FullDayMinutes = ISNULL(FullDayMinutes, @Full),
            WorkHours      = ISNULL(WorkHours,      @Hours);

        PRINT 'Shift settings copied onto existing HrSchoolTiming rows.';
    END
END
GO

-- 3. Designation on the day rows ----------------------------------------------
-- Denormalised from the parent range so attendance can look a day up by
-- (date, designation) without a join, which is the query it runs per employee.
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('HrSchoolTimingDay') AND name = 'DesignationId')
BEGIN
    ALTER TABLE HrSchoolTimingDay ADD DesignationId INT NULL;
    PRINT 'HrSchoolTimingDay.DesignationId added.';
END
GO

UPDATE D
SET    D.DesignationId = T.DesignationId
FROM   HrSchoolTimingDay D
JOIN   HrSchoolTiming    T ON T.Id = D.TimingId
WHERE  ISNULL(D.DesignationId, -1) <> ISNULL(T.DesignationId, -1);
GO

-- A date is no longer unique on its own: one date can now carry a row per
-- designation plus the school-wide one.
IF EXISTS (SELECT 1 FROM sys.indexes
           WHERE name = 'UX_HrSchTmDay_Date' AND object_id = OBJECT_ID('HrSchoolTimingDay'))
BEGIN
    DROP INDEX UX_HrSchTmDay_Date ON HrSchoolTimingDay;
    PRINT 'UX_HrSchTmDay_Date dropped.';
END
GO

-- Filtered unique indexes, one per case: SQL Server treats NULLs as equal in a
-- unique index, so a single index over (TDate, DesignationId) would allow only
-- ONE school-wide row across all dates.
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'UX_HrSchTmDay_Date_Desig' AND object_id = OBJECT_ID('HrSchoolTimingDay'))
BEGIN
    CREATE UNIQUE INDEX UX_HrSchTmDay_Date_Desig
        ON HrSchoolTimingDay(TDate, DesignationId)
        WHERE DesignationId IS NOT NULL;
    PRINT 'UX_HrSchTmDay_Date_Desig created.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'UX_HrSchTmDay_Date_All' AND object_id = OBJECT_ID('HrSchoolTimingDay'))
BEGIN
    CREATE UNIQUE INDEX UX_HrSchTmDay_Date_All
        ON HrSchoolTimingDay(TDate)
        WHERE DesignationId IS NULL;
    PRINT 'UX_HrSchTmDay_Date_All created.';
END
GO

-- 4. Retire HrShift -----------------------------------------------------------
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HrShift')
   AND NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HrShift_Backup_2026_09')
BEGIN
    SELECT * INTO HrShift_Backup_2026_09 FROM HrShift;
    PRINT 'HrShift backed up to HrShift_Backup_2026_09.';
END
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_HrAtt_Shift')
    ALTER TABLE HrAttendance DROP CONSTRAINT FK_HrAtt_Shift;
GO
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Employees_HrShift')
    ALTER TABLE Employees DROP CONSTRAINT FK_Employees_HrShift;
GO

IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('HrAttendance') AND name = 'ShiftId')
BEGIN
    ALTER TABLE HrAttendance DROP COLUMN ShiftId;
    PRINT 'HrAttendance.ShiftId dropped.';
END
GO

IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('Employees') AND name = 'ShiftId')
BEGIN
    ALTER TABLE Employees DROP COLUMN ShiftId;
    PRINT 'Employees.ShiftId dropped.';
END
GO

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HrShift')
BEGIN
    DROP TABLE HrShift;
    PRINT 'HrShift dropped (rows kept in HrShift_Backup_2026_09).';
END
GO

-- 5. Report -------------------------------------------------------------------
SELECT 'HrSchoolTiming rows: ' + CAST(COUNT(*) AS VARCHAR) +
       ', designation-specific: ' +
       CAST(SUM(CASE WHEN DesignationId IS NOT NULL THEN 1 ELSE 0 END) AS VARCHAR)
FROM HrSchoolTiming;
GO

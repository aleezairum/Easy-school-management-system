/* ============================================================================
   School Timings: weekly off days.

   A school-wide start/end time should not be applied to days the school is
   shut. Holidays were already dropped when a range is expanded; the weekly off
   is the other half of that — Sunday, or Saturday and Sunday, depending on the
   school.

     HrSchoolTiming.OffDay1 / OffDay2   1 = Sunday .. 7 = Saturday, the same
                                        convention HrShift and Employees already
                                        use. NULL = no weekly off.

   Those weekdays simply do not get a day row, so there is no school timing on
   them and attendance falls back to each employee's own weekly-off handling —
   exactly what happens on a date no timing covers.

   Idempotent: safe to re-run. Existing timings get NULL, i.e. no weekly off,
   which is what they were saved as.

   >>> After running this, re-save any existing timing whose weekly offs you
       want dropped — the day rows already written are not touched here. <<<

   Target: SQL Server 2019
   ============================================================================ */

USE [EasySchoolDB]
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('HrSchoolTiming','U') IS NOT NULL
BEGIN
    IF COL_LENGTH('HrSchoolTiming','OffDay1') IS NULL
        ALTER TABLE HrSchoolTiming ADD OffDay1 TINYINT NULL;   -- 1=Sun .. 7=Sat
    IF COL_LENGTH('HrSchoolTiming','OffDay2') IS NULL
        ALTER TABLE HrSchoolTiming ADD OffDay2 TINYINT NULL;
END
GO

IF OBJECT_ID('HrSchoolTiming','U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_HrSchTm_OffDays')
    ALTER TABLE HrSchoolTiming ADD CONSTRAINT CK_HrSchTm_OffDays CHECK
    (
        (OffDay1 IS NULL OR OffDay1 BETWEEN 1 AND 7)
        AND (OffDay2 IS NULL OR OffDay2 BETWEEN 1 AND 7)
        -- The same day twice is a mistake, not a second off day.
        AND (OffDay1 IS NULL OR OffDay2 IS NULL OR OffDay1 <> OffDay2)
    );
GO


/* ---------------------------------------------------------------------------
   Result — the weekdays each timing's days currently land on, so you can see
   which ones a re-save would now drop.
   --------------------------------------------------------------------------- */
SELECT  T.Id, T.Name, T.StartDate, T.EndDate, T.OffDay1, T.OffDay2,
        DATENAME(weekday, D.TDate) AS Weekday,
        COUNT(*) AS Days
FROM    HrSchoolTiming T
JOIN    HrSchoolTimingDay D ON D.TimingId = T.Id
GROUP BY T.Id, T.Name, T.StartDate, T.EndDate, T.OffDay1, T.OffDay2,
         DATENAME(weekday, D.TDate), DATEPART(weekday, D.TDate)
ORDER BY T.Id, DATEPART(weekday, D.TDate);
GO

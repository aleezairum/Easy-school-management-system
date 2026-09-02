/* ============================================================================
   School Timings: Friday timings, and per-day edits.

   A single start/end time for a whole date range does not survive contact with
   a real school: Friday runs short, and every so often one particular day moves.
   Two additions, both on top of the existing tables:

     HrSchoolTiming.FridayTimeIn / FridayTimeOut
         Optional. When set, Fridays inside the range get these instead of the
         main times. NULL means Friday is like any other day.

     HrSchoolTimingDay.IsOverridden
         Set when someone edits that ONE day by hand. Re-saving the range
         rebuilds every other day from the header but leaves these alone — an
         edit made on purpose should not be silently undone by re-entering the
         range it sits in.

   The range is still how timings are entered; the day rows are still what
   attendance reads. Nothing about that changes.

   Idempotent: safe to re-run. Existing day rows get IsOverridden = 0, which is
   correct — they were all generated from their range.

   Target: SQL Server 2019
   ============================================================================ */

USE [EasySchoolDB]
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('HrSchoolTiming','U') IS NOT NULL
BEGIN
    IF COL_LENGTH('HrSchoolTiming','FridayTimeIn')  IS NULL
        ALTER TABLE HrSchoolTiming ADD FridayTimeIn  TIME(0) NULL;
    IF COL_LENGTH('HrSchoolTiming','FridayTimeOut') IS NULL
        ALTER TABLE HrSchoolTiming ADD FridayTimeOut TIME(0) NULL;
END
GO

IF OBJECT_ID('HrSchoolTiming','U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_HrSchTm_FriTimes')
    ALTER TABLE HrSchoolTiming ADD CONSTRAINT CK_HrSchTm_FriTimes CHECK
    (
        -- Both set and in order, or neither set at all.
        (FridayTimeIn IS NULL AND FridayTimeOut IS NULL)
        OR (FridayTimeIn IS NOT NULL AND FridayTimeOut IS NOT NULL AND FridayTimeOut > FridayTimeIn)
    );
GO

IF OBJECT_ID('HrSchoolTimingDay','U') IS NOT NULL
   AND COL_LENGTH('HrSchoolTimingDay','IsOverridden') IS NULL
    ALTER TABLE HrSchoolTimingDay ADD IsOverridden BIT NOT NULL
        CONSTRAINT DF_HrSchTmDay_Ovr DEFAULT (0);
GO


/* ---------------------------------------------------------------------------
   Result
   --------------------------------------------------------------------------- */
SELECT  T.Id, T.Name, T.StartDate, T.EndDate,
        T.TimeIn, T.TimeOut, T.FridayTimeIn, T.FridayTimeOut,
        COUNT(D.Id)                                        AS DaysApplied,
        SUM(CASE WHEN D.IsOverridden = 1 THEN 1 ELSE 0 END) AS DaysEditedByHand
FROM    HrSchoolTiming T
LEFT JOIN HrSchoolTimingDay D ON D.TimingId = T.Id
GROUP BY T.Id, T.Name, T.StartDate, T.EndDate, T.TimeIn, T.TimeOut, T.FridayTimeIn, T.FridayTimeOut
ORDER BY T.StartDate DESC;
GO

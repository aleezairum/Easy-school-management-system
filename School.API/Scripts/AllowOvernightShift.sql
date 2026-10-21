/* ============================================================================
   Night shifts — a timing that runs past midnight
   ============================================================================

   Deliberately NO "USE [EasySchoolDB]" here: a hard-coded USE overrides
   sqlcmd's -d and sends the script to whatever that name resolves to on the
   machine it runs on. The database comes from -d instead.

     cd School.API\Scripts
     sqlcmd -S . -U sa -P <pwd> -d EasySchoolDB -i AllowOvernightShift.sql

   The guard works 7:00 PM to 7:00 AM. Two CHECK constraints made that
   impossible to save:

       CK_HrSchTm_Times     TimeOut > TimeIn
       CK_HrSchTm_FriTimes  FridayTimeOut > FridayTimeIn

   Both are replaced with "not equal". An end BEFORE the start is not a
   mistake, it is the next morning; an end EQUAL to the start is still refused,
   because that describes either nothing at all or a full 24 hours.

   Nothing is dropped and no row is touched: every timing that satisfied the
   old rule satisfies the new one.
   ============================================================================ */
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_HrSchTm_Times')
    ALTER TABLE dbo.HrSchoolTiming DROP CONSTRAINT CK_HrSchTm_Times;
GO

ALTER TABLE dbo.HrSchoolTiming WITH CHECK
    ADD CONSTRAINT CK_HrSchTm_Times CHECK ([TimeOut] <> [TimeIn]);
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_HrSchTm_FriTimes')
    ALTER TABLE dbo.HrSchoolTiming DROP CONSTRAINT CK_HrSchTm_FriTimes;
GO

ALTER TABLE dbo.HrSchoolTiming WITH CHECK
    ADD CONSTRAINT CK_HrSchTm_FriTimes CHECK
    (
        ([FridayTimeIn] IS NULL AND [FridayTimeOut] IS NULL)
        OR ([FridayTimeIn] IS NOT NULL AND [FridayTimeOut] IS NOT NULL AND [FridayTimeOut] <> [FridayTimeIn])
    );
GO

SELECT 'CK_HrSchTm_Times    : ' + ISNULL((SELECT definition FROM sys.check_constraints WHERE name = 'CK_HrSchTm_Times'), 'missing');
SELECT 'CK_HrSchTm_FriTimes : ' + ISNULL((SELECT definition FROM sys.check_constraints WHERE name = 'CK_HrSchTm_FriTimes'), 'missing');
GO

-- Adds JobId column + supporting index to SmsMessages for the SMS queue
-- processor (SmsQueueProcessor). Run once against EasySchoolDB.
-- Safe to re-run: guarded with IF NOT EXISTS checks.

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.SmsMessages') AND name = 'JobId'
)
BEGIN
    ALTER TABLE dbo.SmsMessages ADD JobId NVARCHAR(100) NULL;
END
GO

-- Supports the queue processor's "top N pending ordered by InsertedDate"
-- scan and the per-job progress count query.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.SmsMessages')
      AND name = 'IX_SmsMessages_Status_InsertedDate'
)
BEGIN
    CREATE INDEX IX_SmsMessages_Status_InsertedDate
        ON dbo.SmsMessages (Status, InsertedDate);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.SmsMessages')
      AND name = 'IX_SmsMessages_JobId'
)
BEGIN
    CREATE INDEX IX_SmsMessages_JobId
        ON dbo.SmsMessages (JobId)
        WHERE JobId IS NOT NULL;
END
GO

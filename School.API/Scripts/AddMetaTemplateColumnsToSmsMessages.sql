USE [EasySchoolDB]
GO
/* Filtered indexes require these ON. sqlcmd defaults QUOTED_IDENTIFIER to OFF,
   so set them explicitly rather than relying on the client's defaults. */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
/* ============================================================================
   Adds Meta WhatsApp Cloud API (template) columns to SmsMessages.

   Business-initiated WhatsApp messages sent through Meta's Cloud API must use
   a pre-approved template plus an ordered list of body parameters, instead of
   free-form text. These columns record exactly what was sent (template name,
   language, parameters), the fully rendered message the parent receives, and
   the provider's delivery outcome.

   EVERY column is NULLABLE and NO existing column is modified or dropped —
   legacy rows and the older providers (WAHA / Wasender / Arsal) keep working
   unchanged and simply leave these NULL.

   Idempotent — safe to re-run.
   ============================================================================ */

IF COL_LENGTH('dbo.SmsMessages', 'TemplateName') IS NULL
    ALTER TABLE dbo.SmsMessages ADD TemplateName NVARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.SmsMessages', 'TemplateLanguage') IS NULL
    ALTER TABLE dbo.SmsMessages ADD TemplateLanguage NVARCHAR(10) NULL;
GO
/* JSON array of the body parameters ({{1}}, {{2}}, ...) in order. */
IF COL_LENGTH('dbo.SmsMessages', 'TemplateParams') IS NULL
    ALTER TABLE dbo.SmsMessages ADD TemplateParams NVARCHAR(MAX) NULL;
GO
/* The complete rendered message (template body with parameters substituted).
   MessageText stays capped at 1000 chars for backwards compatibility. */
IF COL_LENGTH('dbo.SmsMessages', 'FullMessage') IS NULL
    ALTER TABLE dbo.SmsMessages ADD FullMessage NVARCHAR(MAX) NULL;
GO
/* Which sender delivered the row: Meta | Waha | Wasender | Arsal | Log. */
IF COL_LENGTH('dbo.SmsMessages', 'Provider') IS NULL
    ALTER TABLE dbo.SmsMessages ADD Provider NVARCHAR(30) NULL;
GO
/* Meta's message id ("wamid.xxx") used to correlate delivery webhooks. */
IF COL_LENGTH('dbo.SmsMessages', 'ProviderMessageId') IS NULL
    ALTER TABLE dbo.SmsMessages ADD ProviderMessageId NVARCHAR(150) NULL;
GO
/* Fine-grained delivery state: accepted | sent | delivered | read | failed.
   The existing Status column stays the coarse queue state (0/1/2). */
IF COL_LENGTH('dbo.SmsMessages', 'DeliveryStatus') IS NULL
    ALTER TABLE dbo.SmsMessages ADD DeliveryStatus NVARCHAR(20) NULL;
GO
IF COL_LENGTH('dbo.SmsMessages', 'DeliveredAt') IS NULL
    ALTER TABLE dbo.SmsMessages ADD DeliveredAt DATETIME2 NULL;
GO
IF COL_LENGTH('dbo.SmsMessages', 'ReadAt') IS NULL
    ALTER TABLE dbo.SmsMessages ADD ReadAt DATETIME2 NULL;
GO
/* Meta error code (e.g. 132000 = wrong parameter count). */
IF COL_LENGTH('dbo.SmsMessages', 'ErrorCode') IS NULL
    ALTER TABLE dbo.SmsMessages ADD ErrorCode NVARCHAR(50) NULL;
GO
/* Raw JSON returned by the provider — invaluable when a send fails. */
IF COL_LENGTH('dbo.SmsMessages', 'ProviderResponse') IS NULL
    ALTER TABLE dbo.SmsMessages ADD ProviderResponse NVARCHAR(MAX) NULL;
GO

/* Delivery webhooks look rows up by Meta's message id. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_SmsMessages_ProviderMessageId'
                 AND object_id = OBJECT_ID('dbo.SmsMessages'))
    CREATE NONCLUSTERED INDEX IX_SmsMessages_ProviderMessageId
        ON dbo.SmsMessages (ProviderMessageId)
        WHERE ProviderMessageId IS NOT NULL;
GO

/* The messages viewer filters/sorts on these. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_SmsMessages_Status_InsertedDate'
                 AND object_id = OBJECT_ID('dbo.SmsMessages'))
    CREATE NONCLUSTERED INDEX IX_SmsMessages_Status_InsertedDate
        ON dbo.SmsMessages (Status, InsertedDate DESC);
GO

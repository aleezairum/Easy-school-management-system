USE [EasySchoolDB]
GO
/* Filtered/unique indexes require these ON. sqlcmd defaults QUOTED_IDENTIFIER
   to OFF, so set them explicitly rather than relying on the client. */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
/* ============================================================================
   Two-way WhatsApp chat storage.

   Meta does NOT keep conversation history for us and exposes no API to fetch
   it — a thread exists only because we stored each webhook event as it
   arrived. These tables are therefore the sole record of incoming messages.

   WaConversations : one row per contact (chat thread)
   WaMessages      : every message in either direction

   Idempotent — safe to re-run.
   ============================================================================ */

IF OBJECT_ID('dbo.WaConversations', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.WaConversations
    (
        VID                   INT IDENTITY(1,1) NOT NULL,
        -- Digits with country code, no '+', e.g. 923001234567
        WaId                  NVARCHAR(20)  NOT NULL,
        -- Name from the contact's WhatsApp profile (supplied by Meta)
        ProfileName           NVARCHAR(200) NULL,
        -- Name resolved from our own student/parent/staff records
        ContactName           NVARCHAR(200) NULL,
        LastMessageAt         DATETIME2     NULL,
        LastMessagePreview    NVARCHAR(500) NULL,
        LastMessageDirection  INT           NULL,
        -- Meta's 24-hour service window runs from the last INBOUND message
        LastInboundAt         DATETIME2     NULL,
        UnreadCount           INT           NOT NULL CONSTRAINT DF_WaConversations_Unread DEFAULT (0),
        InsertedDate          DATETIME2     NOT NULL CONSTRAINT DF_WaConversations_Ins   DEFAULT (SYSUTCDATETIME()),
        UpdatedDate           DATETIME2     NULL,
        CONSTRAINT PK_WaConversations PRIMARY KEY CLUSTERED (VID)
    );
END
GO

/* One thread per contact. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'UX_WaConversations_WaId'
                 AND object_id = OBJECT_ID('dbo.WaConversations'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_WaConversations_WaId
        ON dbo.WaConversations (WaId);
GO

/* The chat list is ordered by most recent activity. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_WaConversations_LastMessageAt'
                 AND object_id = OBJECT_ID('dbo.WaConversations'))
    CREATE NONCLUSTERED INDEX IX_WaConversations_LastMessageAt
        ON dbo.WaConversations (LastMessageAt DESC);
GO

IF OBJECT_ID('dbo.WaMessages', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.WaMessages
    (
        VID               INT IDENTITY(1,1) NOT NULL,
        ConversationId    INT            NOT NULL,
        WaId              NVARCHAR(20)   NOT NULL,
        -- 0 = Inbound (from contact), 1 = Outbound (from school)
        Direction         INT            NOT NULL,
        -- Meta's "wamid.xxx"; also used to de-duplicate webhook retries
        MessageId         NVARCHAR(150)  NULL,
        -- text | image | document | audio | video | sticker | location | template
        MessageKind       NVARCHAR(30)   NOT NULL CONSTRAINT DF_WaMessages_Kind DEFAULT ('text'),
        Body              NVARCHAR(MAX)  NULL,
        MediaId           NVARCHAR(150)  NULL,
        MediaMimeType     NVARCHAR(100)  NULL,
        MediaFileName     NVARCHAR(260)  NULL,
        -- Outbound: accepted | sent | delivered | read | failed
        Status            NVARCHAR(20)   NULL,
        ErrorCode         NVARCHAR(50)   NULL,
        ErrorMessage      NVARCHAR(500)  NULL,
        -- wamid this message replies to, when the contact used WhatsApp's reply
        ContextMessageId  NVARCHAR(150)  NULL,
        MessageTime       DATETIME2      NOT NULL CONSTRAINT DF_WaMessages_Time DEFAULT (SYSUTCDATETIME()),
        DeliveredAt       DATETIME2      NULL,
        ReadAt            DATETIME2      NULL,
        -- Whether school staff have opened this inbound message
        IsRead            BIT            NOT NULL CONSTRAINT DF_WaMessages_IsRead DEFAULT (0),
        -- Links a mirrored row back to the campaign row in SmsMessages
        SmsMessageVID     INT            NULL,
        TemplateName      NVARCHAR(100)  NULL,
        InsertedBy        INT            NULL,
        InsertedDate      DATETIME2      NOT NULL CONSTRAINT DF_WaMessages_Ins DEFAULT (SYSUTCDATETIME()),
        UpdatedDate       DATETIME2      NULL,
        CONSTRAINT PK_WaMessages PRIMARY KEY CLUSTERED (VID),
        CONSTRAINT FK_WaMessages_Conversation FOREIGN KEY (ConversationId)
            REFERENCES dbo.WaConversations (VID)
    );
END
GO

/* Thread view: newest messages of one conversation. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_WaMessages_Conversation_Time'
                 AND object_id = OBJECT_ID('dbo.WaMessages'))
    CREATE NONCLUSTERED INDEX IX_WaMessages_Conversation_Time
        ON dbo.WaMessages (ConversationId, MessageTime DESC);
GO

/* Meta retries webhooks, so an inbound wamid must only ever be stored once.
   Filtered because outbound rows can briefly have a NULL MessageId. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'UX_WaMessages_MessageId'
                 AND object_id = OBJECT_ID('dbo.WaMessages'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_WaMessages_MessageId
        ON dbo.WaMessages (MessageId)
        WHERE MessageId IS NOT NULL;
GO

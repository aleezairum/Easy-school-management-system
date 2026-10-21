/* ============================================================================
   Daily Diary and Announcements
   ============================================================================

   Deliberately NO "USE [EasySchoolDB]" here: a hard-coded USE overrides
   sqlcmd's -d. The database comes from -d instead.

     cd School.API\Scripts
     sqlcmd -S . -U sa -P <pwd> -d EasySchoolDB -i DailyDiaryAndAnnouncements.sql

   DAILY DIARY
     SMSDailyDiary        one diary per date, class and section. SectionID
                          NULL means every section of the class.
     SMSDailyDiaryImage   its pages - photos of the diary, one row each.
                          Kept in the database, like staff photos, so the
                          nightly backup carries them.
     A parent asks for "Today's Diary" on WhatsApp and gets the images for
     their child's class and section.

   ANNOUNCEMENTS
     SMSAnnouncement      notices for the whole school, shown on WhatsApp
                          from StartDate to EndDate (NULL = until switched
                          off). Audience 0 = everyone, 1 = parents only,
                          2 = staff only. CampusID NULL = both campuses.
                          An optional image goes out with the text.

   Additive and safe to re-run.
   ============================================================================ */
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('dbo.SMSDailyDiary', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SMSDailyDiary
    (
        VID           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SMSDailyDiary PRIMARY KEY,
        DiaryDate     DATE           NOT NULL,
        ClassID       INT            NOT NULL CONSTRAINT FK_SMSDD_Class REFERENCES dbo.SMSClass(VID),
        SectionID     INT            NULL     CONSTRAINT FK_SMSDD_Section REFERENCES dbo.SMSSection(VID),
        -- A line for the caption, e.g. "Homework: Urdu page 12".
        Notes         NVARCHAR(500)  NULL,
        InsertedBy    INT            NULL,
        InsertedDate  DATETIME       NOT NULL CONSTRAINT DF_SMSDD_Inserted DEFAULT (GETDATE()),
        UpdatedBy     INT            NULL,
        UpdatedDate   DATETIME       NULL
    );
END
GO

-- One diary per date + class + section. A filtered pair, because a unique
-- index treats NULL as one value: "all sections" may exist once per class.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_SMSDD_Section')
    CREATE UNIQUE INDEX UX_SMSDD_Section ON dbo.SMSDailyDiary (DiaryDate, ClassID, SectionID) WHERE SectionID IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_SMSDD_AllSections')
    CREATE UNIQUE INDEX UX_SMSDD_AllSections ON dbo.SMSDailyDiary (DiaryDate, ClassID) WHERE SectionID IS NULL;
GO

IF OBJECT_ID('dbo.SMSDailyDiaryImage', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SMSDailyDiaryImage
    (
        VID           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SMSDailyDiaryImage PRIMARY KEY,
        DiaryID       INT            NOT NULL CONSTRAINT FK_SMSDDI_Diary REFERENCES dbo.SMSDailyDiary(VID) ON DELETE CASCADE,
        SortOrder     INT            NOT NULL CONSTRAINT DF_SMSDDI_Sort DEFAULT (0),
        FileName      NVARCHAR(200)  NOT NULL,
        ContentType   NVARCHAR(50)   NOT NULL,
        ImageData     VARBINARY(MAX) NOT NULL,
        InsertedBy    INT            NULL,
        InsertedDate  DATETIME       NOT NULL CONSTRAINT DF_SMSDDI_Inserted DEFAULT (GETDATE())
    );
    CREATE INDEX IX_SMSDDI_Diary ON dbo.SMSDailyDiaryImage (DiaryID, SortOrder);
END
GO

IF OBJECT_ID('dbo.SMSAnnouncement', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SMSAnnouncement
    (
        VID               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SMSAnnouncement PRIMARY KEY,
        Title             NVARCHAR(200)  NOT NULL,
        Message           NVARCHAR(MAX)  NOT NULL,
        StartDate         DATE           NOT NULL,
        EndDate           DATE           NULL,
        -- 0 everyone, 1 parents, 2 staff.
        Audience          TINYINT        NOT NULL CONSTRAINT DF_SMSAnn_Audience DEFAULT (0)
                                         CONSTRAINT CK_SMSAnn_Audience CHECK (Audience IN (0, 1, 2)),
        CampusID          INT            NULL,
        ImageFileName     NVARCHAR(200)  NULL,
        ImageContentType  NVARCHAR(50)   NULL,
        ImageData         VARBINARY(MAX) NULL,
        IsActive          BIT            NOT NULL CONSTRAINT DF_SMSAnn_Active DEFAULT (1),
        InsertedBy        INT            NULL,
        InsertedDate      DATETIME       NOT NULL CONSTRAINT DF_SMSAnn_Inserted DEFAULT (GETDATE()),
        UpdatedBy         INT            NULL,
        UpdatedDate       DATETIME       NULL,
        CONSTRAINT CK_SMSAnn_Dates CHECK (EndDate IS NULL OR EndDate >= StartDate)
    );
    CREATE INDEX IX_SMSAnn_Dates ON dbo.SMSAnnouncement (IsActive, StartDate, EndDate);
END
GO

SELECT 'Daily diary   : ' + CASE WHEN OBJECT_ID('dbo.SMSDailyDiaryImage') IS NULL THEN 'missing' ELSE 'ready' END;
SELECT 'Announcements : ' + CASE WHEN OBJECT_ID('dbo.SMSAnnouncement') IS NULL THEN 'missing' ELSE 'ready' END;
GO

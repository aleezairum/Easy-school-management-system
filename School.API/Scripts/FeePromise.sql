/* ============================================================================
   Promise to pay — parents who commit to clearing dues by a date
   ============================================================================

   Deliberately NO "USE [EasySchoolDB]" here: a hard-coded USE overrides
   sqlcmd's -d and sends the script to whatever that name resolves to on the
   machine it runs on. The database comes from -d instead.

     cd School.API\Scripts
     sqlcmd -S . -U sa -P <pwd> -d EasySchoolDB -i FeePromise.sql

   The case: a parent comes in and says "I will clear the dues by the 20th,
   please do not keep asking the child". Until the 20th that family should drop
   off the pending-fee list and receive no fee reminders. On the 21st, if the
   dues are still there, they are back on the list on their own — nothing to
   run, nothing to remember.

   FAMILY, not child: the promise is recorded against one student but covers
   every sibling of the same parent. The father pays for all his children at
   one counter and has one phone number; a promise that silenced one child and
   let the reminder go out for the other would be worse than none.

   What this creates:

     SMSFeePromise        one row per promise. A student can promise more than
                          once over a year, so this is a history, not a flag on
                          the student.

     vwActiveFeePromise   THE definition of "a promise is in force today". Three
                          different places have to agree about that — the unpaid
                          report, the reminder message list, and the automatic
                          monthly reminder — so the rule is written once here
                          and those three ask the view.

     SpSave/Delete/Get_SMSFeePromise   the form behind it.

   The promise date is INCLUSIVE: a promise for the 20th still holds all day on
   the 20th and lapses on the 21st.
   ============================================================================ */
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* ── 1. The promises ────────────────────────────────────────────────────── */

IF OBJECT_ID('dbo.SMSFeePromise', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SMSFeePromise
    (
        VID            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SMSFeePromise PRIMARY KEY,
        StudentID      INT           NOT NULL,
        PromiseDate    DATE          NOT NULL,
        PromisedAmount DECIMAL(18,2) NULL,        -- what they said they would pay; blank = all of it
        PromisedBy     NVARCHAR(200) NULL,        -- who gave the word: father, mother, guardian
        Remarks        NVARCHAR(500) NULL,
        IsActive       BIT           NOT NULL CONSTRAINT DF_SMSFeePromise_IsActive DEFAULT (1),

        InsertedBy     INT           NULL,
        InsertedDate   DATETIME      NOT NULL CONSTRAINT DF_SMSFeePromise_Inserted DEFAULT (GETDATE()),
        InsertedIp     NVARCHAR(45)  NULL,
        UpdatedBy      INT           NULL,
        UpdatedDate    DATETIME      NULL,
        UpdatedIp      NVARCHAR(45)  NULL,

        CONSTRAINT FK_SMSFeePromise_Student FOREIGN KEY (StudentID) REFERENCES dbo.SMSStudent (VID)
    );

    -- Every read is "has this student got one in force", so the index leads on
    -- the student and carries the date.
    CREATE INDEX IX_SMSFeePromise_Student_Date ON dbo.SMSFeePromise (StudentID, PromiseDate) INCLUDE (IsActive);
END
GO

/* ── 2. What "in force" means ───────────────────────────────────────────── */

IF OBJECT_ID('dbo.vwActiveFeePromise', 'V') IS NOT NULL
    DROP VIEW dbo.vwActiveFeePromise;
GO

IF OBJECT_ID('dbo.vwActiveFeePromiseDetail', 'V') IS NOT NULL
    DROP VIEW dbo.vwActiveFeePromiseDetail;
GO

CREATE VIEW dbo.vwActiveFeePromiseDetail
AS
    /* One row per student covered by a promise TODAY, carrying WHICH promise
       covers them - the date, who gave their word, and how much.

       The Unpaid Fee report prints these next to the student, so that a sheet
       showing a promised family says on its face why nobody has chased them
       and when the money is expected.

       A family with two promises gets the one that runs longest; ties go to
       the one recorded last. That is the same winner vwActiveFeePromise has
       always picked with MAX(PromiseDate), which is now defined on top of this
       so the sibling rule lives in exactly one place. */
    SELECT  X.StudentID,
            X.PromiseID,
            X.PromiseDate,
            X.PromisedBy,
            X.PromisedAmount,
            X.PromiseRemarks,
            X.PromisedForName
    FROM (
        SELECT  Sib.VID         AS StudentID,
                P.VID           AS PromiseID,
                P.PromiseDate,
                P.PromisedBy,
                P.PromisedAmount,
                P.Remarks       AS PromiseRemarks,
                Promised.Name   AS PromisedForName,
                ROW_NUMBER() OVER (PARTITION BY Sib.VID
                                   ORDER BY P.PromiseDate DESC, P.VID DESC) AS rn
        FROM    dbo.SMSFeePromise P
        JOIN    dbo.SMSStudent Promised ON Promised.VID = P.StudentID
        JOIN    dbo.SMSStudent Sib
               ON Sib.VID = Promised.VID
               OR (Promised.ParentID IS NOT NULL AND Sib.ParentID = Promised.ParentID)
        WHERE   P.IsActive = 1
          AND   P.PromiseDate >= CAST(GETDATE() AS DATE)
    ) X
    WHERE X.rn = 1;
GO

CREATE VIEW dbo.vwActiveFeePromise
AS
    /* One row per student who is covered by a promise TODAY.

       A promise covers the WHOLE FAMILY, not the one child it was recorded
       against. A father who says "I will clear it by the 20th" is speaking for
       every child he has in the school — he pays at one counter, the fee
       voucher already settles all his children together, and the reminders go
       to his one phone number. Chasing his other child the same afternoon
       makes a liar of the office.

       So the promise is taken on a student and read on the family: the row is
       spread to every sibling sharing that ParentID. A child with no parent
       record on file is covered on their own.

       A family with two promises gets the one that runs longest — the office
       granting more time should not be undone by an older, shorter promise
       still sitting in the table.

       Two columns only, because that is all its three readers want: the unpaid
       report, the reminder message list and the monthly reminder each ask "is
       this student covered, and until when". Anything that needs the promise
       itself reads vwActiveFeePromiseDetail, which this is built on so the
       sibling rule is written in exactly one place. */
    SELECT  StudentID,
            PromiseDate
    FROM    dbo.vwActiveFeePromiseDetail;
GO

/* ── 3. The form behind it ──────────────────────────────────────────────── */

IF OBJECT_ID('dbo.SpSave_SMSFeePromise', 'P') IS NULL
    EXEC('CREATE PROCEDURE dbo.SpSave_SMSFeePromise AS SET NOCOUNT ON;');
GO

ALTER PROCEDURE dbo.SpSave_SMSFeePromise
(
    @VID            INT           = 0,
    @StudentID      INT,
    @PromiseDate    DATE,
    @PromisedAmount DECIMAL(18,2) = NULL,
    @PromisedBy     NVARCHAR(200) = NULL,
    @Remarks        NVARCHAR(500) = NULL,
    @IsActive       BIT           = 1,
    @UserID         INT           = 0,
    @UserIP         NVARCHAR(45)  = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.SMSStudent WHERE VID = @StudentID)
    BEGIN
        SELECT CAST(0 AS DECIMAL(18,0)) AS VID, 400 AS ReturnCode, 'Student not found' AS ReturnMessage;
        RETURN;
    END

    -- A promise for a date already gone protects nobody and only confuses the
    -- list, so it is refused at the door rather than saved and ignored.
    IF @PromiseDate < CAST(GETDATE() AS DATE)
    BEGIN
        SELECT CAST(0 AS DECIMAL(18,0)) AS VID, 400 AS ReturnCode,
               'The promise date has already passed. Pick today or a later date.' AS ReturnMessage;
        RETURN;
    END

    IF @VID IS NULL OR @VID = 0
    BEGIN
        INSERT dbo.SMSFeePromise
            (StudentID, PromiseDate, PromisedAmount, PromisedBy, Remarks, IsActive, InsertedBy, InsertedDate, InsertedIp)
        VALUES
            (@StudentID, @PromiseDate, @PromisedAmount, @PromisedBy, @Remarks, @IsActive, @UserID, GETDATE(), @UserIP);

        SELECT CAST(SCOPE_IDENTITY() AS DECIMAL(18,0)) AS VID, 200 AS ReturnCode, 'Promise saved' AS ReturnMessage;
        RETURN;
    END

    UPDATE dbo.SMSFeePromise
       SET StudentID      = @StudentID,
           PromiseDate    = @PromiseDate,
           PromisedAmount = @PromisedAmount,
           PromisedBy     = @PromisedBy,
           Remarks        = @Remarks,
           IsActive       = @IsActive,
           UpdatedBy      = @UserID,
           UpdatedDate    = GETDATE(),
           UpdatedIp      = @UserIP
     WHERE VID = @VID;

    IF @@ROWCOUNT = 0
        SELECT CAST(0 AS DECIMAL(18,0)) AS VID, 404 AS ReturnCode, 'Promise not found' AS ReturnMessage;
    ELSE
        SELECT CAST(@VID AS DECIMAL(18,0)) AS VID, 200 AS ReturnCode, 'Promise updated' AS ReturnMessage;
END
GO

IF OBJECT_ID('dbo.SpDelete_SMSFeePromise', 'P') IS NULL
    EXEC('CREATE PROCEDURE dbo.SpDelete_SMSFeePromise AS SET NOCOUNT ON;');
GO

ALTER PROCEDURE dbo.SpDelete_SMSFeePromise
(
    @VID    INT,
    @UserID INT          = 0,
    @UserIP NVARCHAR(45) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.SMSFeePromise WHERE VID = @VID;

    IF @@ROWCOUNT = 0
        SELECT CAST(0 AS DECIMAL(18,0)) AS VID, 404 AS ReturnCode, 'Promise not found' AS ReturnMessage;
    ELSE
        SELECT CAST(@VID AS DECIMAL(18,0)) AS VID, 200 AS ReturnCode, 'Promise removed' AS ReturnMessage;
END
GO

IF OBJECT_ID('dbo.SpGet_SMSFeePromise', 'P') IS NULL
    EXEC('CREATE PROCEDURE dbo.SpGet_SMSFeePromise AS SET NOCOUNT ON;');
GO

ALTER PROCEDURE dbo.SpGet_SMSFeePromise
(
    @VID        INT = NULL,
    @StudentID  INT = NULL,
    @ClassIDs   NVARCHAR(MAX) = NULL,
    @CampusIDs  NVARCHAR(MAX) = NULL,
    @Status     TINYINT = 0      -- 0 all, 1 in force, 2 lapsed and still owing, 3 settled
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Today DATE = CAST(GETDATE() AS DATE);

    ;WITH Dues AS
    (
        -- What the FAMILY still owes as at today. Family, because that is what
        -- the promise covers — showing one child's share next to a promise that
        -- holds off every sibling would misstate what was promised.
        --
        -- "As at today" is the same cut the rest of the application makes: a
        -- fee raised for next month is not yet owed.
        -- IsClear = 0 as well, so this agrees to the rupee with
        -- SpGetPending_SMSStudentFee, which is where the figure the form
        -- prints when the promise is taken comes from.
        SELECT  S.ParentID,
                SUM(F.FeeAmount - F.ClearedAmount) AS PendingAmount
        FROM    dbo.SMSStudentFee F
        JOIN    dbo.SMSStudent    S ON S.VID = F.StudentID
        WHERE   F.IsActive = 1
          AND   F.IsClear  = 0
          AND   F.FeeAmount - F.ClearedAmount > 0
          AND   CAST(F.VDate AS DATE) <= @Today
          AND   S.ParentID IS NOT NULL
        GROUP BY S.ParentID
    ),
    OwnDues AS
    (
        -- The lone child with no parent record on file has nobody to share a
        -- promise with, so their own dues stand in for the family's.
        SELECT  F.StudentID,
                SUM(F.FeeAmount - F.ClearedAmount) AS PendingAmount
        FROM    dbo.SMSStudentFee F
        WHERE   F.IsActive = 1
          AND   F.IsClear  = 0
          AND   F.FeeAmount - F.ClearedAmount > 0
          AND   CAST(F.VDate AS DATE) <= @Today
        GROUP BY F.StudentID
    ),
    Family AS
    (
        -- How many children the promise actually spares from being chased —
        -- the ones who owe, not every child ever entered against the parent.
        --
        -- The two are not the same: a family can have five children on file
        -- and dues against four, the fifth having left or paid up. Counting
        -- all five made this column disagree with the line the form prints
        -- when the promise is taken, which names the children who owe.
        SELECT  S.ParentID,
                COUNT(DISTINCT F.StudentID) AS SiblingCount
        FROM    dbo.SMSStudentFee F
        JOIN    dbo.SMSStudent    S ON S.VID = F.StudentID
        WHERE   F.IsActive = 1
          AND   F.IsClear  = 0
          AND   F.FeeAmount - F.ClearedAmount > 0
          AND   CAST(F.VDate AS DATE) <= @Today
          AND   S.ParentID IS NOT NULL
        GROUP BY S.ParentID
    )
    SELECT  P.VID,
            P.StudentID,
            S.RollNo,
            S.Name              AS StudentName,
            PR.FatherName,
            PR.FatherCNIC,
            PR.FatherMobile     AS ContactNo,
            C.VName             AS ClassName,
            SEC.VName           AS SectionName,
            CP.VName            AS CampusName,
            P.PromiseDate,
            P.PromisedAmount,
            P.PromisedBy,
            P.Remarks,
            P.IsActive,
            X.DueNow AS PendingAmount,
            CAST(ISNULL(FM.SiblingCount, 1) AS INT) AS SiblingCount,
            DATEDIFF(DAY, @Today, P.PromiseDate) AS DaysLeft,

            /* The three states the office cares about, worked out here so the
               screen and the reports cannot disagree about them:
                 1  in force   — the date has not passed; the family is off the
                                 pending list and gets no reminders
                 2  lapsed     — the date went by and the dues are still there;
                                 the family is back on the pending list
                 3  settled    — nothing owing any more, whatever the date */
            CAST(CASE WHEN X.DueNow <= 0 THEN 3
                      WHEN P.IsActive = 1 AND P.PromiseDate >= @Today THEN 1
                      ELSE 2
                 END AS TINYINT) AS PromiseStatus
    FROM    dbo.SMSFeePromise P
    JOIN    dbo.SMSStudent          S   ON S.VID        = P.StudentID
    LEFT JOIN dbo.SMSStudentAcademic SA ON SA.StudentID = S.VID
    LEFT JOIN dbo.SMSParent         PR  ON PR.VID       = S.ParentID
    LEFT JOIN dbo.SMSSection        SEC ON SEC.VID      = SA.SectionID
    LEFT JOIN dbo.SMSClass          C   ON C.VID        = SEC.ClassID
    LEFT JOIN dbo.SMSCampus         CP  ON CP.VID       = S.CampusID
    LEFT JOIN Dues                  D   ON D.ParentID   = S.ParentID
    LEFT JOIN OwnDues               O   ON O.StudentID  = P.StudentID
    LEFT JOIN Family                FM  ON FM.ParentID  = S.ParentID
    CROSS APPLY (
        SELECT DueNow = CASE WHEN S.ParentID IS NULL
                             THEN ISNULL(O.PendingAmount, 0)
                             ELSE ISNULL(D.PendingAmount, 0) END
    ) X
    WHERE  (@VID       IS NULL OR @VID       = 0 OR P.VID       = @VID)
      AND  (@StudentID IS NULL OR @StudentID = 0 OR P.StudentID = @StudentID)
      AND  (@ClassIDs  IS NULL OR @ClassIDs  = '' OR SEC.ClassID IN (SELECT Value FROM dbo.SplitString(@ClassIDs, ',')))
      AND  (@CampusIDs IS NULL OR @CampusIDs = '' OR S.CampusID  IN (SELECT Value FROM dbo.SplitString(@CampusIDs, ',')))
      AND  (@Status = 0
            OR @Status = CASE WHEN X.DueNow <= 0 THEN 3
                              WHEN P.IsActive = 1 AND P.PromiseDate >= @Today THEN 1
                              ELSE 2 END)
    ORDER BY P.PromiseDate, S.Name;
END
GO

SELECT 'SMSFeePromise       : ' + CASE WHEN OBJECT_ID('dbo.SMSFeePromise') IS NULL THEN 'missing' ELSE 'ready' END;
SELECT 'vwActiveFeePromise  : ' + CASE WHEN OBJECT_ID('dbo.vwActiveFeePromise') IS NULL THEN 'missing' ELSE 'ready' END;
SELECT 'promises in force   : ' + CAST((SELECT COUNT(*) FROM dbo.vwActiveFeePromise) AS VARCHAR);
GO

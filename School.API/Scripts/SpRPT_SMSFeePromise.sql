/* ============================================================================
   SpRPT_SMSFeePromise - the printed Promise to Pay report
   ============================================================================

   Deliberately NO "USE [EasySchoolDB]" here: a hard-coded USE overrides
   sqlcmd's -d and sends the script to whatever that name resolves to on the
   machine it runs on. The database comes from -d instead.

     cd School.API\Scripts
     sqlcmd -S . -U sa -P <pwd> -d EasySchoolDB -i SpRPT_SMSFeePromise.sql

   HOW THIS DIFFERS FROM SpGet_SMSFeePromise
   -----------------------------------------
   The screen's list shows one row per PROMISE, because each row has an Edit
   and a Delete button and those belong to the promise.

   The printed report shows one row per STUDENT the promise covers. A promise
   is taken against one child and holds for every sibling of that parent, so a
   sheet listing only the child who was written down would send the office
   chasing the other three. The office prints this to know who not to chase.

   WHO IS LISTED
   -------------
   Every child of that parent who is either still enrolled or still owes
   something. A sibling who left years ago and owes nothing is not printed:
   the promise covers them in the sense that nothing is chased, but they are
   not part of the day's work.

   THE FIGURES
   -----------
     OwnDues     what THIS child owes as at today
     FamilyDues  what the whole family owes as at today - the figure the
                 promise was taken against, repeated on each row so any single
                 row can be read on its own

   Both cut at today on the same rule as the rest of the application: a fee
   raised for a month still to come is not yet owed.

     @Status     0 all, 1 in force, 2 lapsed and still owing, 3 settled
     @ClassIDs   comma separated; filters the STUDENT's class, so printing
                 "Class 5" gives the covered children who are in class 5
     @CampusIDs  comma separated
   ============================================================================ */
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('dbo.SpRPT_SMSFeePromise', 'P') IS NULL
    EXEC('CREATE PROCEDURE dbo.SpRPT_SMSFeePromise AS SET NOCOUNT ON;');
GO

ALTER PROCEDURE [dbo].[SpRPT_SMSFeePromise]
(
    @Status    TINYINT       = 0,
    @ClassIDs  NVARCHAR(MAX) = NULL,
    @CampusIDs NVARCHAR(MAX) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Today DATE = CAST(GETDATE() AS DATE);

    ;WITH StudentDues AS
    (
        SELECT  F.StudentID,
                SUM(F.FeeAmount - F.ClearedAmount) AS PendingAmount
        FROM    dbo.SMSStudentFee F
        WHERE   F.IsActive = 1
          AND   F.IsClear  = 0
          AND   F.FeeAmount - F.ClearedAmount > 0
          AND   CAST(F.VDate AS DATE) <= @Today
        GROUP BY F.StudentID
    ),
    FamilyDues AS
    (
        SELECT  S.ParentID,
                SUM(D.PendingAmount) AS PendingAmount
        FROM    StudentDues D
        JOIN    dbo.SMSStudent S ON S.VID = D.StudentID
        WHERE   S.ParentID IS NOT NULL
        GROUP BY S.ParentID
    ),
    Covered AS
    (
        /* The promise, spread to every child it holds for. The join mirrors
           vwActiveFeePromise exactly, so the sheet and the reports cannot
           disagree about who is covered. */
        SELECT  P.VID           AS PromiseID,
                P.PromiseDate,
                P.PromisedAmount,
                P.PromisedBy,
                P.Remarks,
                P.IsActive,
                Promised.Name   AS PromisedForName,
                Sib.VID         AS StudentID,
                Sib.ParentID
        FROM    dbo.SMSFeePromise P
        JOIN    dbo.SMSStudent Promised ON Promised.VID = P.StudentID
        JOIN    dbo.SMSStudent Sib
               ON Sib.VID = Promised.VID
               OR (Promised.ParentID IS NOT NULL AND Sib.ParentID = Promised.ParentID)
    )
    SELECT
        C.PromiseID,
        C.PromiseDate,
        C.PromisedAmount,
        C.PromisedBy,
        C.Remarks,
        C.IsActive,
        C.PromisedForName,

        C.StudentID,
        S.RollNo,
        S.Name              AS StudentName,
        A.RollNo            AS ClassRollNo,
        CL.VName            AS ClassName,
        SEC.VName           AS SectionName,
        CP.VName            AS CampusName,
        PR.FatherName,
        PR.FatherMobile     AS ContactNo,

        CAST(ISNULL(SD.PendingAmount, 0) AS DECIMAL(18,2)) AS OwnDues,
        CAST(X.DueNow AS DECIMAL(18,2))                    AS FamilyDues,

        DATEDIFF(DAY, @Today, C.PromiseDate) AS DaysLeft,

        /* Judged on the FAMILY's dues, because that is what the promise
           covers. 1 in force, 2 lapsed and still owing, 3 settled. */
        CAST(CASE WHEN X.DueNow <= 0 THEN 3
                  WHEN C.IsActive = 1 AND C.PromiseDate >= @Today THEN 1
                  ELSE 2
             END AS TINYINT) AS PromiseStatus

    FROM    Covered C
    JOIN    dbo.SMSStudent           S   ON S.VID        = C.StudentID
    LEFT JOIN dbo.SMSStudentAcademic A   ON A.StudentID  = S.VID AND A.StatusID = 1
    LEFT JOIN dbo.SMSSection         SEC ON SEC.VID      = A.SectionID
    LEFT JOIN dbo.SMSClass           CL  ON CL.VID       = SEC.ClassID
    LEFT JOIN dbo.SMSParent          PR  ON PR.VID       = S.ParentID
    LEFT JOIN dbo.SMSCampus          CP  ON CP.VID       = S.CampusID
    LEFT JOIN StudentDues            SD  ON SD.StudentID = S.VID
    LEFT JOIN FamilyDues             FD  ON FD.ParentID  = S.ParentID
    CROSS APPLY (
        SELECT DueNow = CASE WHEN S.ParentID IS NULL
                             THEN ISNULL(SD.PendingAmount, 0)
                             ELSE ISNULL(FD.PendingAmount, 0) END
    ) X

    -- Still enrolled, or still owing. A sibling who left and owes nothing is
    -- covered by the promise but is not part of the day's chasing.
    WHERE (A.StudentID IS NOT NULL OR SD.PendingAmount > 0)
      AND (@ClassIDs  IS NULL OR @ClassIDs  = '' OR SEC.ClassID IN (SELECT Value FROM dbo.SplitString(@ClassIDs, ',')))
      AND (@CampusIDs IS NULL OR @CampusIDs = '' OR S.CampusID  IN (SELECT Value FROM dbo.SplitString(@CampusIDs, ',')))
      AND (@Status = 0
           OR @Status = CASE WHEN X.DueNow <= 0 THEN 3
                             WHEN C.IsActive = 1 AND C.PromiseDate >= @Today THEN 1
                             ELSE 2 END)

    -- Grouped the way it is printed: a block per promise, the children in
    -- roll order inside it.
    ORDER BY C.PromiseDate, C.PromiseID, CL.VName, TRY_CAST(A.RollNo AS INT), S.Name;
END
GO

SELECT 'SpRPT_SMSFeePromise: ' +
       CASE WHEN OBJECT_ID('dbo.SpRPT_SMSFeePromise') IS NULL
            THEN 'NOT created' ELSE 'ready' END;
GO

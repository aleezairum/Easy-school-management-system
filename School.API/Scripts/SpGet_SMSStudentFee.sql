/* ============================================================================
   SpGet_SMSStudentFee - the Apply Fee List screen
   ============================================================================

   Deliberately NO "USE [EasySchoolDB]" here: a hard-coded USE overrides
   sqlcmd's -d and sends the script to whatever that name resolves to on the
   machine it runs on. The database comes from -d instead.

     cd School.API\Scripts
     sqlcmd -S . -U sa -P <pwd> -d EasySchoolDB -i SpGet_SMSStudentFee.sql

   WHAT CHANGED AND WHY
   --------------------
   Only one thing: the result now carries IsDue.

   StudentFeeListDto is shared by this procedure and by
   SpGetPending_SMSStudentFee. When IsDue was added to that DTO (so the Receive
   Payment screen could tell arrears from months raised in advance), this
   procedure was left behind, and EF Core refuses a keyless result set that is
   missing a mapped column:

     The required column 'IsDue' was not present in the results of a 'FromSql'
     operation.

   The Apply Fee List answered 500 from that moment on:

     GET /api/ApplyFee?dateFrom=2026-09-01&dateTo=2026-09-30 -> 500

   IsDue means the same thing here as everywhere else: 1 when the fee is owed
   as at today, 0 when it is raised for a month still to come. The screen is
   free to ignore it; the column simply has to be there.
   ============================================================================ */
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('dbo.SpGet_SMSStudentFee', 'P') IS NULL
    EXEC('CREATE PROCEDURE dbo.SpGet_SMSStudentFee AS SET NOCOUNT ON;');
GO

ALTER PROCEDURE [dbo].[SpGet_SMSStudentFee]
(
    @VID       INT = NULL,
    @StudentID INT = NULL,
    @SessionID INT = NULL,
    @SectionID INT = NULL,
    @DateFrom  DATETIME,
    @DateTo    DATETIME
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Today DATE = CAST(GETDATE() AS DATE);

    SELECT
        F.VID,
        F.VDate,
        S.Name       AS StudentName,
        C.VName      AS ClassName,
        SY.VName     AS SessionName,
        SEC.VName    AS SectionName,
        G.VName      AS GradeName,
        FT.VName     AS FeeTypeName,
        F.FeeAmount,
        F.ClearedAmount,
        F.FeeAmount - F.ClearedAmount AS PendingAmount,
        F.Remarks,
        F.IsClear,

        -- 1 = owed today. 0 = raised for a month still to come.
        CAST(CASE WHEN CAST(F.VDate AS DATE) <= @Today THEN 1 ELSE 0 END AS BIT) AS IsDue

    FROM SMSStudentFee F
    LEFT JOIN SMSStudent          S   ON F.StudentID = S.VID
    LEFT JOIN AcademicSessionYear SY  ON F.SessionID = SY.VID
    LEFT JOIN SMSSection          SEC ON F.SectionID = SEC.VID
    LEFT JOIN SMSClass            C   ON SEC.ClassID = C.VID
    LEFT JOIN AcademicGrades      G   ON F.GradeID   = G.VID
    LEFT JOIN SMSFeeType          FT  ON F.FeeTypeID = FT.VID

    WHERE F.VDate BETWEEN @DateFrom AND @DateTo
      AND (@VID       IS NULL OR @VID       = 0 OR F.VID       = @VID)
      AND (@StudentID IS NULL OR @StudentID = 0 OR F.StudentID = @StudentID)
      AND (@SessionID IS NULL OR @SessionID = 0 OR F.SessionID = @SessionID)
      AND (@SectionID IS NULL OR @SectionID = 0 OR F.SectionID = @SectionID)
      AND F.IsActive = 1

    ORDER BY F.VDate DESC;
END
GO

SELECT 'SpGet_SMSStudentFee: ' +
       CASE WHEN OBJECT_DEFINITION(OBJECT_ID('dbo.SpGet_SMSStudentFee')) LIKE '%IsDue%'
            THEN 'updated - returns IsDue, Apply Fee List works again'
            ELSE 'NOT updated' END;
GO

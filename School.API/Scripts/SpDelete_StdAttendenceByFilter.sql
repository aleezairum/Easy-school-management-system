USE [EasySchoolDB]
GO
/****** Object: StoredProcedure [dbo].[SpDelete_StdAttendenceByFilter] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF OBJECT_ID('dbo.SpDelete_StdAttendenceByFilter', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SpDelete_StdAttendenceByFilter;
GO

-- Deletes attendance rows matching the SAME filter parameters used by SpGet_StdAttendence.
-- Typical use: delete one class/section's attendance for a single day
-- (pass @SessionID, @SectionID and @DateFrom = @DateTo = the day).
CREATE PROCEDURE [dbo].[SpDelete_StdAttendenceByFilter]
(
    @VID        INT = NULL,
    @SessionID  INT = NULL,
    @StudentID  INT = NULL,
    @SectionID  INT = NULL,
    @DateFrom   DATETIME = NULL,
    @DateTo     DATETIME = NULL,
    @UserID     INT = NULL,
    @UserIP     NVARCHAR(50) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    -- Safety guard: require a Section and a date range so we can never wipe the whole table.
    IF (@SectionID IS NULL OR @SectionID = 0) OR @DateFrom IS NULL OR @DateTo IS NULL
    BEGIN
        SELECT CAST(0 AS DECIMAL(18,0)) AS VID,
               400 AS ReturnCode,
               'Section and date range are required to delete attendance.' AS ReturnMessage;
        RETURN;
    END

    DECLARE @Cnt INT = 0;

    DELETE FROM StdAttendence
    WHERE CAST(VDate AS DATE) BETWEEN CAST(@DateFrom AS DATE) AND CAST(@DateTo AS DATE)
      AND (@VID       IS NULL OR @VID       = 0 OR VID       = @VID)
      AND (@SessionID IS NULL OR @SessionID = 0 OR SessionID = @SessionID)
      AND (@StudentID IS NULL OR @StudentID = 0 OR StudentID = @StudentID)
      AND (@SectionID IS NULL OR @SectionID = 0 OR SectionID = @SectionID);

    SET @Cnt = @@ROWCOUNT;

    IF @Cnt = 0
    BEGIN
        SELECT CAST(0 AS DECIMAL(18,0)) AS VID,
               404 AS ReturnCode,
               'No attendance records found for the selected class, section and date.' AS ReturnMessage;
        RETURN;
    END

    SELECT CAST(0 AS DECIMAL(18,0)) AS VID,
           200 AS ReturnCode,
           CAST(@Cnt AS NVARCHAR(10)) + ' attendance record(s) deleted.' AS ReturnMessage;
END
GO

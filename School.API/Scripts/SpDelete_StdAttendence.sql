USE [EasySchoolDB]
GO
/****** Object: StoredProcedure [dbo].[SpDelete_StdAttendence] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- Soft-deletes a single attendance row by VID.
-- NOTE: VID is returned as DECIMAL to match DTOs.Common.ResponseDto.VID (decimal);
-- returning a plain INT caused an int->decimal cast failure (HTTP 500) when mapped by EF.
ALTER PROCEDURE [dbo].[SpDelete_StdAttendence]
(
    @VID INT,
    @UserID INT,
    @UserIP NVARCHAR(50)
)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        UPDATE StdAttendence
        SET
            IsActive = 0,
            UpdatedBy = @UserID,
            UpdatedDate = GETDATE(),
            UpdatedIp = @UserIP
        WHERE VID = @VID;

        SELECT CAST(@VID AS DECIMAL(18,0)) AS VID, 200 AS ReturnCode, 'Attendance Deleted Successfully' AS ReturnMessage;
    END TRY
    BEGIN CATCH
        SELECT CAST(0 AS DECIMAL(18,0)) AS VID, 500 AS ReturnCode, ERROR_MESSAGE() AS ReturnMessage;
    END CATCH
END
GO

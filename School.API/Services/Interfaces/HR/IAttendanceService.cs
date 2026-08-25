using School.API.DTOs.HR;

namespace School.API.Services.Interfaces.HR
{
    public interface IAttendanceService
    {
        Task<AttendanceMonthGridDto> GetMonthGridAsync(int year, int month, int? designationId);
        Task SaveBulkAsync(SaveAttendanceBulkDto dto, int? userId);
        Task FillAsync(FillAttendanceDto dto, int? userId);
    }
}

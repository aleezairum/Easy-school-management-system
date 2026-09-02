using School.API.DTOs.HR;

namespace School.API.Services.Interfaces.HR
{
    public interface IAttendanceService
    {
        Task<AttendanceMonthGridDto> GetMonthGridAsync(int year, int month, int? designationId);

        // One date across all employees — daily entry, carrying that date's
        // school timings.
        Task<AttendanceDayGridDto> GetDayGridAsync(DateTime date, int? designationId);
        Task SaveBulkAsync(SaveAttendanceBulkDto dto, int? userId);
        Task FillAsync(FillAttendanceDto dto, int? userId);
    }
}

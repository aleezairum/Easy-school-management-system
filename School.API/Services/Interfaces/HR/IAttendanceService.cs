using School.API.DTOs.HR;

namespace School.API.Services.Interfaces.HR
{
    public interface IAttendanceService
    {
        Task<AttendanceMonthGridDto> GetMonthGridAsync(int year, int month, List<int>? designationIds, int? campusId = null, string? sortBy = null);

        // One date across all employees — daily entry, carrying that date's
        // school timings.
        Task<AttendanceDayGridDto> GetDayGridAsync(DateTime date, List<int>? designationIds, int? campusId = null, string? sortBy = null);
        Task SaveBulkAsync(SaveAttendanceBulkDto dto, int? userId);

        /// <summary>Works saved attendance out again against today's timings.</summary>
        Task<AttendanceRecalculateResultDto> RecalculateAsync(DateTime from, DateTime to, int? userId);
        Task FillAsync(FillAttendanceDto dto, int? userId);
    }
}

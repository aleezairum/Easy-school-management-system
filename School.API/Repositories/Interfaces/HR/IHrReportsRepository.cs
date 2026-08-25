using School.API.DTOs.HR;

namespace School.API.Repositories.Interfaces.HR
{
    public interface IHrReportsRepository
    {
        Task<List<DailyAttendanceReportRowDto>> GetDailyAsync(DateTime date, int? designationId);
        Task<List<MonthlyAttendanceSummaryRowDto>> GetMonthlySummaryAsync(int year, int month, int? designationId);
        Task<List<LateComerRowDto>> GetLateComersAsync(DateTime from, DateTime to, int? designationId);
        Task<List<AbsenteeRowDto>> GetAbsenteesAsync(DateTime from, DateTime to, int? designationId);
    }
}

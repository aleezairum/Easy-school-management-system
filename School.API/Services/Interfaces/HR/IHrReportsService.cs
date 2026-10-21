using School.API.DTOs.HR;

namespace School.API.Services.Interfaces.HR
{
    /// <summary>
    /// The staff attendance reports and the attendance card, with the late,
    /// early and out-of-school minutes priced the way payroll charges them.
    /// </summary>
    public interface IHrReportsService
    {
        /// <param name="type">daily, late, early, outing or absent.</param>
        Task<List<AttendanceReportRowDto>> GetAttendanceRowsAsync(string type, DateTime from, DateTime to, HrReportFilter filter);

        Task<List<AttendanceSummaryRowDto>> GetAttendanceSummaryAsync(DateTime from, DateTime to, HrReportFilter filter);

        Task<List<AttendanceCardDto>> GetAttendanceCardsAsync(DateTime from, DateTime to, HrReportFilter filter);
    }
}

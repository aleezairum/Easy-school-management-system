using School.API.DTOs.HR;

namespace School.API.Repositories.Interfaces.HR
{
    public interface IHrReportsRepository
    {
        /// <summary>Saved attendance rows in a date range, for the staff the filter picks.</summary>
        Task<List<AttendanceReportRowDto>> GetAttendanceRowsAsync(DateTime from, DateTime to, HrReportFilter filter);

        /// <summary>The hours each day was meant to run, per designation.</summary>
        Task<List<TimingDayRow>> GetTimingDaysAsync(DateTime from, DateTime to);

        /// <summary>
        /// The Staff Statement as of a date — every employee with their
        /// qualification, appointment date and the monthly salary in force on
        /// that date, oldest appointment first.
        /// </summary>
        /// <param name="asOn">
        /// The "with effect from" date printed on the sheet. Pay structures are
        /// dated, so this decides which salary is quoted — asking for last April
        /// gives last April's figures, not today's.
        /// </param>
        /// <param name="campusId">
        /// Only staff posted to this campus. Staff with no campus recorded are
        /// left out — the sheet names a campus in its heading, so listing
        /// someone under it whose posting nobody has entered would be a claim
        /// the data does not support.
        /// </param>
        Task<List<StaffStatementRowDto>> GetStaffStatementAsync(
            DateTime asOn, int? designationId, bool includeInactive, int? campusId = null,
            int? employeeId = null, DateTime? joinedFrom = null, DateTime? joinedTo = null);
    }
}

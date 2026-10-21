using School.API.DTOs.Reports;

namespace School.API.Repositories.Interfaces.Reports
{
    public interface ICustomReportRepository
    {
        /// <summary>
        /// Runs a caller-composed report. Throws <see cref="ArgumentException"/>
        /// for an unknown entity or an empty column set; unknown column and
        /// operator keys are dropped rather than throwing, so one stale filter
        /// in a saved layout does not fail the whole report.
        /// </summary>
        Task<CustomReportResultDto> RunAsync(CustomReportRequestDto request);
    }
}

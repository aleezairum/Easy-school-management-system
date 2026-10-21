using School.API.DTOs.Reports;

namespace School.API.Services.Interfaces.Reports
{
    public interface ICustomReportService
    {
        /// <summary>
        /// What the screen may offer: every selectable column for an entity,
        /// and the operators each one accepts.
        /// </summary>
        ReportSchemaDto GetSchema(string entity);

        Task<CustomReportResultDto> RunAsync(CustomReportRequestDto request);
    }
}

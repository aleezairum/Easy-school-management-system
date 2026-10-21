using School.API.DTOs.Reports;
using School.API.Repositories.Interfaces.Reports;
using School.API.Services.Interfaces.Reports;

namespace School.API.Services.Implementations.Reports
{
    public class CustomReportService : ICustomReportService
    {
        private readonly ICustomReportRepository _repo;

        public CustomReportService(ICustomReportRepository repo)
        {
            _repo = repo;
        }

        public ReportSchemaDto GetSchema(string entity)
        {
            var key = (entity ?? string.Empty).Trim().ToLowerInvariant();
            if (!ReportFieldCatalog.IsKnownEntity(key))
                throw new ArgumentException("Unknown report type.", nameof(entity));

            return new ReportSchemaDto
            {
                Entity = key,
                Fields = ReportFieldCatalog.Fields(key).Select(f => new ReportFieldDto
                {
                    Key = f.Key,
                    Label = f.Label,
                    Group = f.Group,
                    Type = f.Type.ToString().ToLowerInvariant(),
                    Default = f.Default,
                    Lookup = f.Lookup,

                    // Sent per field so the screen never offers "contains" on a
                    // date. The same test runs again server-side — this is a
                    // convenience for the user, not the guard.
                    Operators = ReportFieldCatalog.Operators
                        .Where(o => o.AppliesTo.Contains(f.Type))
                        .Select(o => o.Key)
                        .ToList()
                }).ToList(),

                Operators = ReportFieldCatalog.Operators.Select(o => new ReportOperatorDto
                {
                    Key = o.Key,
                    Label = o.Label,
                    ValueCount = o.ValueCount
                }).ToList()
            };
        }

        public Task<CustomReportResultDto> RunAsync(CustomReportRequestDto request)
            => _repo.RunAsync(request);
    }
}

// DTOs/Reports/CustomReportDtos.cs
namespace School.API.DTOs.Reports
{
    /// <summary>One selectable column, as the screen sees it.</summary>
    public class ReportFieldDto
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;

        /// <summary>text | number | date | bool — decides the filter input.</summary>
        public string Type { get; set; } = "text";

        public bool Default { get; set; }

        /// <summary>Names a lookup list for the filter dropdown; null = free text.</summary>
        public string? Lookup { get; set; }

        /// <summary>Operator keys this field accepts, given its type.</summary>
        public List<string> Operators { get; set; } = new();
    }

    public class ReportOperatorDto
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;

        /// <summary>How many value boxes to show — 0, 1 or 2 (between).</summary>
        public int ValueCount { get; set; }
    }

    public class ReportSchemaDto
    {
        public string Entity { get; set; } = string.Empty;
        public List<ReportFieldDto> Fields { get; set; } = new();
        public List<ReportOperatorDto> Operators { get; set; } = new();
    }

    public class CustomReportFilterDto
    {
        public string Field { get; set; } = string.Empty;
        public string Operator { get; set; } = "eq";
        public string? Value { get; set; }

        /// <summary>The second bound, for "between".</summary>
        public string? Value2 { get; set; }
    }

    public class CustomReportRequestDto
    {
        /// <summary>"student" or "employee".</summary>
        public string Entity { get; set; } = string.Empty;

        /// <summary>Field keys, in the order they should appear.</summary>
        public List<string> Columns { get; set; } = new();

        /// <summary>All applied together (AND).</summary>
        public List<CustomReportFilterDto> Filters { get; set; } = new();

        public string? SortBy { get; set; }

        /// <summary>"asc" or "desc"; anything else is treated as ascending.</summary>
        public string? SortDir { get; set; }

        /// <summary>
        /// Row cap. Clamped server-side — a report builder makes it very easy to
        /// ask for everything, and the browser has to draw whatever comes back.
        /// </summary>
        public int Take { get; set; } = 1000;
    }

    public class CustomReportResultDto
    {
        /// <summary>The chosen columns, resolved to labels and types.</summary>
        public List<ReportFieldDto> Columns { get; set; } = new();

        /// <summary>One entry per row, keyed by field key.</summary>
        public List<Dictionary<string, object?>> Rows { get; set; } = new();

        public int RowCount { get; set; }

        /// <summary>True when the cap cut the result short.</summary>
        public bool Truncated { get; set; }

        /// <summary>Human-readable summary of the filters, for the printed header.</summary>
        public List<string> AppliedFilters { get; set; } = new();
    }
}

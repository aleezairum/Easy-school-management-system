using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.DTOs.Reports;
using School.API.Repositories.Interfaces.Reports;
using School.API.Services.Implementations.Reports;

namespace School.API.Repositories.Implementations.Reports
{
    /// <summary>
    /// Runs a caller-composed report against the whitelisted field catalogue.
    /// </summary>
    /// <remarks>
    /// The one rule this class exists to enforce: <b>no caller-supplied string
    /// ever reaches the SQL text</b>.
    ///
    /// Column names and the FROM clause are looked up in
    /// <see cref="ReportFieldCatalog"/> — a key that is not in the catalogue is
    /// rejected before any SQL is built. Operators come from the catalogue's
    /// template list. Values are always SqlParameters, never concatenated, so
    /// even a value containing quotes or a semicolon is just a value.
    ///
    /// ADO.NET rather than EF, because the shape is not known until runtime —
    /// the caller picks the columns — and there is no entity to map to.
    /// </remarks>
    public class CustomReportRepository : ICustomReportRepository
    {
        private readonly SchoolDbContext _context;
        private readonly ILogger<CustomReportRepository> _logger;

        // A report builder makes "give me everything" a two-click mistake, and
        // the browser has to render whatever comes back. 5000 rows is already
        // more than anyone reads on paper.
        private const int MaxRows = 5000;

        public CustomReportRepository(SchoolDbContext context, ILogger<CustomReportRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CustomReportResultDto> RunAsync(CustomReportRequestDto request)
        {
            var entity = (request.Entity ?? string.Empty).Trim().ToLowerInvariant();
            if (!ReportFieldCatalog.IsKnownEntity(entity))
                throw new ArgumentException("Unknown report type.", nameof(request));

            // ── Columns ──────────────────────────────────────────────────
            var columns = request.Columns
                .Select(key => ReportFieldCatalog.Find(entity, key))
                .Where(f => f is not null)
                .Select(f => f!)
                .DistinctBy(f => f.Key)
                .ToList();

            if (columns.Count == 0)
                columns = ReportFieldCatalog.Fields(entity).Where(f => f.Default).ToList();

            if (columns.Count == 0)
                throw new ArgumentException("Select at least one column.", nameof(request));

            var take = Math.Clamp(request.Take <= 0 ? 1000 : request.Take, 1, MaxRows);

            var sql = new StringBuilder();
            var parameters = new List<SqlParameter>();
            var applied = new List<string>();

            // The column alias is the field KEY, and a key is [A-Za-z] by
            // construction — it comes from the catalogue, not from the caller.
            sql.Append("SELECT TOP (").Append(take + 1).Append(") ");
            sql.AppendJoin(", ", columns.Select(c => $"{c.Sql} AS [{c.Key}]"));
            sql.AppendLine().Append(ReportFieldCatalog.From(entity));

            // ── Filters ──────────────────────────────────────────────────
            var where = new List<string>();

            foreach (var filter in request.Filters ?? new List<CustomReportFilterDto>())
            {
                var field = ReportFieldCatalog.Find(entity, filter.Field);
                if (field is null) continue;

                var op = ReportFieldCatalog.FindOperator(filter.Operator);
                if (op is null) continue;

                // "contains" on a date, "between" on a name — nonsense that SQL
                // would either reject or answer strangely. Drop it.
                if (!op.AppliesTo.Contains(field.Type)) continue;

                if (op.ValueCount >= 1 && string.IsNullOrWhiteSpace(filter.Value)) continue;
                if (op.ValueCount == 2 && string.IsNullOrWhiteSpace(filter.Value2)) continue;

                var names = new string[op.ValueCount];
                for (var i = 0; i < op.ValueCount; i++)
                {
                    var raw = i == 0 ? filter.Value : filter.Value2;
                    var name = $"@p{parameters.Count}";

                    parameters.Add(TypedParameter(name, field.Type, raw!));
                    names[i] = name;
                }

                where.Add(op.ValueCount switch
                {
                    0 => string.Format(op.Template, field.Sql),
                    1 => string.Format(op.Template, field.Sql, names[0]),
                    _ => string.Format(op.Template, field.Sql, names[0], names[1])
                });

                applied.Add(op.ValueCount switch
                {
                    0 => $"{field.Label} {op.Label}",
                    1 => $"{field.Label} {op.Label} {filter.Value}",
                    _ => $"{field.Label} {op.Label} {filter.Value} and {filter.Value2}"
                });
            }

            if (where.Count > 0)
                sql.AppendLine().Append("WHERE ").AppendJoin(" AND ", where);

            // ── Sort ─────────────────────────────────────────────────────
            var sortField = ReportFieldCatalog.Find(entity, request.SortBy);
            var descending = string.Equals(request.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

            sql.AppendLine().Append("ORDER BY ").Append(
                sortField is not null
                    ? sortField.Sql + (descending ? " DESC" : " ASC")
                    : ReportFieldCatalog.DefaultSort(entity));

            // ── Run ──────────────────────────────────────────────────────
            var result = new CustomReportResultDto
            {
                Columns = columns.Select(c => new ReportFieldDto
                {
                    Key = c.Key,
                    Label = c.Label,
                    Group = c.Group,
                    Type = c.Type.ToString().ToLowerInvariant()
                }).ToList(),
                AppliedFilters = applied
            };

            await using var cn = new SqlConnection(_context.Database.GetConnectionString());
            await cn.OpenAsync();

            await using var cmd = new SqlCommand(sql.ToString(), cn);
            cmd.Parameters.AddRange(parameters.ToArray());

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                // One row past the cap proves there is more, without a second
                // COUNT query over the same joins.
                if (result.Rows.Count == take) { result.Truncated = true; break; }

                var row = new Dictionary<string, object?>(columns.Count);
                for (var i = 0; i < columns.Count; i++)
                    row[columns[i].Key] = reader.IsDBNull(i) ? null : reader.GetValue(i);

                result.Rows.Add(row);
            }

            result.RowCount = result.Rows.Count;
            return result;
        }

        /// <summary>
        /// A parameter typed by the FIELD, not by what the value looks like.
        /// </summary>
        /// <remarks>
        /// Sending every value as a string works until it does not: SQL Server
        /// then converts on its side, which drops the index on the column and,
        /// for dates, applies the SERVER's date format to a string the browser
        /// wrote — so 03/04 means March in one place and April in another. The
        /// catalogue already knows the type; use it.
        ///
        /// A value that will not parse is passed through as text rather than
        /// throwing. It then matches nothing, which is the honest answer to a
        /// filter that says nothing.
        /// </remarks>
        private static SqlParameter TypedParameter(string name, ReportFieldType type, string raw)
        {
            var value = raw.Trim();

            switch (type)
            {
                case ReportFieldType.Number when decimal.TryParse(value, out var number):
                    return new SqlParameter(name, System.Data.SqlDbType.Decimal)
                    {
                        Precision = 18,
                        Scale = 4,
                        Value = number
                    };

                case ReportFieldType.Date when DateTime.TryParse(
                        value,
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out var date):
                    return new SqlParameter(name, System.Data.SqlDbType.DateTime2) { Value = date };

                case ReportFieldType.Bool:
                    var truthy = value is "1" or "true" or "True" or "yes" or "Yes";
                    return new SqlParameter(name, System.Data.SqlDbType.Bit) { Value = truthy };

                default:
                    return new SqlParameter(name, System.Data.SqlDbType.NVarChar, 4000) { Value = value };
            }
        }
    }
}

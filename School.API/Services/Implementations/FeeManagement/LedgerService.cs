// Services/Implementations/FeeManagement/LedgerService.cs
using School.API.DTOs.FeeManagement;
using School.API.Repositories.Interfaces.Accounts;
using School.API.Services.Interfaces.Accounts;

namespace School.API.Services.Implementations.Accounts
{
    public class LedgerService : ILedgerService
    {
        private readonly ILedgerRepository _repo;

        public LedgerService(ILedgerRepository repo)
        {
            _repo = repo;
        }

        public async Task<LedgerResponseDto> GetStudentLedgerAsync(int studentId, DateTime? dateFrom, DateTime? dateTo)
        {
            var rows = await _repo.GetStudentLedgerAsync(studentId, dateFrom, dateTo);
            return Compose(rows, dateFrom, dateTo, withStudentSummary: false);
        }

        public async Task<LedgerResponseDto> GetFamilyLedgerAsync(int parentId, DateTime? dateFrom, DateTime? dateTo)
        {
            var rows = await _repo.GetFamilyLedgerAsync(parentId, dateFrom, dateTo);
            return Compose(rows, dateFrom, dateTo, withStudentSummary: true);
        }

        public Task<List<FamilyLookupDto>> GetFamiliesAsync(string? search, int top)
            => _repo.GetFamiliesAsync(search, top <= 0 ? 50 : Math.Min(top, 200));

        // Splits the opening-balance line off the transactions and works out
        // the figures the report footer needs. The closing balance is taken
        // from the last row's running balance rather than re-added from the
        // totals, so it can never drift from the line it sits under.
        private static LedgerResponseDto Compose(
            List<LedgerRowDto> rows, DateTime? dateFrom, DateTime? dateTo, bool withStudentSummary)
        {
            var opening = rows.FirstOrDefault(r => r.RowType == 0);
            var lines = rows.Where(r => r.RowType == 1).ToList();

            // The header is stamped on every row by the procedure; an account
            // with no rows at all still yields the opening line.
            var header = opening ?? rows.FirstOrDefault();

            var result = new LedgerResponseDto
            {
                AccountTitle = header?.AccountTitle,
                AccountCode = header?.AccountCode,
                AccountInfo = header?.AccountInfo,
                DateFrom = dateFrom ?? lines.FirstOrDefault()?.LineDate ?? DateTime.Today,
                DateTo = dateTo ?? lines.LastOrDefault()?.LineDate ?? DateTime.Today,
                OpeningBalance = opening?.Balance ?? 0m,
                TotalDebit = lines.Sum(r => r.Debit),
                TotalCredit = lines.Sum(r => r.Credit),
                Rows = lines
            };

            result.ClosingBalance = lines.Count > 0 ? lines[^1].Balance : result.OpeningBalance;

            if (withStudentSummary)
            {
                result.Students = lines
                    .GroupBy(r => new { r.StudentID, r.StudentName })
                    .Select(g => new LedgerStudentSummaryDto
                    {
                        StudentID = g.Key.StudentID,
                        StudentName = g.Key.StudentName,
                        Debit = g.Sum(r => r.Debit),
                        Credit = g.Sum(r => r.Credit),
                        // Per-child balance stands on its own: it is that
                        // child's debits less credits inside the window, not a
                        // slice of the family's running balance.
                        Balance = g.Sum(r => r.Debit) - g.Sum(r => r.Credit)
                    })
                    .OrderBy(s => s.StudentName)
                    .ToList();
            }

            return result;
        }
    }
}

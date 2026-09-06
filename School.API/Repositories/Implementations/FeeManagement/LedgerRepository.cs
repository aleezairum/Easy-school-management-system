// Repositories/Implementations/FeeManagement/LedgerRepository.cs
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.DTOs.FeeManagement;
using School.API.Repositories.Interfaces.Accounts;

namespace School.API.Repositories.Implementations.Accounts
{
    public class LedgerRepository : ILedgerRepository
    {
        private readonly SchoolDbContext _context;

        public LedgerRepository(SchoolDbContext context)
        {
            _context = context;
        }

        // A null date means "no bound" to the procedures, which lets the
        // caller pull a whole account history without picking a start date.
        public async Task<List<LedgerRowDto>> GetStudentLedgerAsync(int studentId, DateTime? dateFrom, DateTime? dateTo)
        {
            return await _context
                .Set<LedgerRowDto>()
                .FromSqlRaw(
                    "EXEC dbo.SpRPT_StudentLedger @StudentID={0}, @DateFrom={1}, @DateTo={2}",
                    studentId,
                    (object?)dateFrom ?? DBNull.Value,
                    (object?)dateTo ?? DBNull.Value)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<LedgerRowDto>> GetFamilyLedgerAsync(int parentId, DateTime? dateFrom, DateTime? dateTo)
        {
            return await _context
                .Set<LedgerRowDto>()
                .FromSqlRaw(
                    "EXEC dbo.SpRPT_FamilyLedger @ParentID={0}, @DateFrom={1}, @DateTo={2}",
                    parentId,
                    (object?)dateFrom ?? DBNull.Value,
                    (object?)dateTo ?? DBNull.Value)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<FamilyLookupDto>> GetFamiliesAsync(string? search, int top)
        {
            return await _context
                .Set<FamilyLookupDto>()
                .FromSqlRaw(
                    "EXEC dbo.SpGet_FamilyLookup @Search={0}, @Top={1}",
                    string.IsNullOrWhiteSpace(search) ? (object)DBNull.Value : search,
                    top)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}

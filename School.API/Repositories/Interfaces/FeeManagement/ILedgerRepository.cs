// Repositories/Interfaces/FeeManagement/ILedgerRepository.cs
using School.API.DTOs.FeeManagement;

namespace School.API.Repositories.Interfaces.Accounts
{
    public interface ILedgerRepository
    {
        Task<List<LedgerRowDto>> GetStudentLedgerAsync(int studentId, DateTime? dateFrom, DateTime? dateTo);
        Task<List<LedgerRowDto>> GetFamilyLedgerAsync(int parentId, DateTime? dateFrom, DateTime? dateTo);
        Task<List<FamilyLookupDto>> GetFamiliesAsync(string? search, int top);
    }
}

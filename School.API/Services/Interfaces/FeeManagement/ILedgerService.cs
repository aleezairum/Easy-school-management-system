// Services/Interfaces/FeeManagement/ILedgerService.cs
using School.API.DTOs.FeeManagement;

namespace School.API.Services.Interfaces.Accounts
{
    public interface ILedgerService
    {
        Task<LedgerResponseDto> GetStudentLedgerAsync(int studentId, DateTime? dateFrom, DateTime? dateTo);
        Task<LedgerResponseDto> GetFamilyLedgerAsync(int parentId, DateTime? dateFrom, DateTime? dateTo);
        Task<List<FamilyLookupDto>> GetFamiliesAsync(string? search, int top);
    }
}

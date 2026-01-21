using School.API.Data.DBModels.Accounts;
using School.API.DTOs;

namespace School.API.Repositories.Interfaces.Accounts
{
    public interface IChallanVoucherRepository
    {
        Task<IEnumerable<ChallanVoucherListDto>> GetAllAsync();
        Task<ChallanVoucherDto?> GetByIdAsync(int id);
        Task<IEnumerable<ChallanVoucherListDto>> GetByStudentAsync(int studentId);
        Task<IEnumerable<ChallanVoucherListDto>> GetByClassSectionAsync(int classId, int sectionId, int sessionId);
        Task<IEnumerable<ChallanDropdownDto>> GetPendingByStudentAsync(int studentId);
        Task<ChallanVoucher> CreateAsync(CreateChallanVoucherDto dto);
        Task<ChallanVoucher?> UpdateAsync(int id, UpdateChallanVoucherDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<string> GenerateChallanNumberAsync();
        Task UpdatePaidAmountAsync(int id, decimal amount);
    }
}

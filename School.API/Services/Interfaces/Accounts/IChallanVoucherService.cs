using School.API.DTOs;

namespace School.API.Services.Interfaces.Accounts
{
    public interface IChallanVoucherService
    {
        Task<IEnumerable<ChallanVoucherListDto>> GetAllAsync();
        Task<ChallanVoucherDto?> GetByIdAsync(int id);
        Task<IEnumerable<ChallanVoucherListDto>> GetByStudentAsync(int studentId);
        Task<IEnumerable<ChallanVoucherListDto>> GetByClassSectionAsync(int classId, int sectionId, int sessionId);
        Task<IEnumerable<ChallanDropdownDto>> GetPendingByStudentAsync(int studentId);
        Task<ChallanVoucherDto> CreateAsync(CreateChallanVoucherDto dto);
        Task<ChallanVoucherDto?> UpdateAsync(int id, UpdateChallanVoucherDto dto);
        Task<bool> DeleteAsync(int id);
    }
}

using School.API.DTOs.FeeManagement;
using School.API.DTOs.Common;

namespace School.API.Repositories.Interfaces.Accounts
{
    public interface IFeeStructureRepository
    {
        Task<ResponseDto> SaveAsync(FeeStructureSaveDto dto, int userId, string userIp);
        Task<List<FeeStructureSaveDto>> GetAllAsync();
        Task<FeeStructureSaveDto?> GetByIdAsync(int id);
        Task<ResponseDto?> DeleteByIdAsync(int id);
    }
}

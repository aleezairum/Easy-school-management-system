using School.API.DTOs.FeeManagement;
using School.API.DTOs.Common;

namespace School.API.Services.Interfaces.Accounts
{
    public interface IFeeStructureService
    {
        Task<ResponseDto> SaveAsync(FeeStructureSaveDto dto, int userId, string userIp);
        Task<List<FeeStructureSaveDto>> GetAllAsync();
        Task<FeeStructureSaveDto?> GetByIdAsync(int id);
        Task<ResponseDto?> DeleteByIdAsync(int id);
    }
}

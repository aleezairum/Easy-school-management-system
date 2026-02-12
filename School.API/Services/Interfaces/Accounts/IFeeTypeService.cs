using School.API.Data.DBModels.Accounts;
using School.API.DTOs.Accounts;
using School.API.DTOs.Common;

namespace School.API.Services.Interfaces.Accounts
{
    public interface IFeeTypeService
    {
        Task<ResponseDto> SaveAsync(FeeTypeSaveDto dto, int userId, string userIp);
        Task<List<FeeType>> GetAllAsync();
        Task<FeeType?> GetByIdAsync(int id);
        Task<ResponseDto?> DeleteByIdAsync(int id);
    }
}

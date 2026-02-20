using School.API.Data.DBModels.Academic;
using School.API.Data.DBModels.Accounts;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.DTOs.FeeManagement;

namespace School.API.Repositories.Interfaces.Accounts
{
    public interface IFeeTypeRepository
    {
        Task<ResponseDto> SaveAsync(
            FeeTypeSaveDto dto,
            int userId,
            string userIp);

        Task<List<SMSFeeType>> GetAllAsync();
        Task<SMSFeeType?> GetByIdAsync(int id);
        Task<ResponseDto?> DeleteByIdAsync(int id);
    }
}

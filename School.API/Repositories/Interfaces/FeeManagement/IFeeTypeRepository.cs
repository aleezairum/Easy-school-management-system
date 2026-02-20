using School.API.Data.DBModels.Academic;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;

namespace School.API.Repositories.Interfaces.Accounts
{
    public interface IFeeTypeRepository
    {
        Task<ResponseDto> SaveAsync(
            SMSClassSaveDto dto,
            int userId,
            string userIp);

        Task<List<SMSFeeType>> GetAllAsync();
        Task<SMSFeeType?> GetByIdAsync(int id);
        Task<ResponseDto?> DeleteByIdAsync(int id);
    }
}

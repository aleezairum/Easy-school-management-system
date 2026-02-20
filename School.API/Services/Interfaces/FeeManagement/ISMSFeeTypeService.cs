using School.API.Data.DBModels.Academic;
using School.API.Data.DBModels.Accounts;
using School.API.DTOs.Accounts;
using School.API.DTOs.Common;
using School.API.DTOs.FeeManagement;

namespace School.API.Services.Interfaces.Accounts
{
    public interface ISMSFeeTypeService
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

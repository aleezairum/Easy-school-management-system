using School.API.Data.DBModels.Academic;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;

namespace School.API.Services.Interfaces.Academic
{
    public interface ISMSClassService
    {
        Task<ResponseDto> SaveAsync(
            SMSClassSaveDto dto,
            int userId,
            string userIp);

        Task<List<SMSClass>> GetAllAsync();
        Task<SMSClass?> GetByIdAsync(int id);
        Task<ResponseDto?> DeleteByIdAsync(int id);
    }
}

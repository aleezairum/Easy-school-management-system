using School.API.Data.DBModels.Academic;
using School.API.Data.DBModels.Accounts;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.DTOs.FeeManagement;

namespace School.API.Services.Interfaces.Academic
{
    public interface ISMSCampusService
    {
        Task<ResponseDto> SaveAsync(
            SMSCampusSaveDto dto,
            int userId,
            string userIp);

        Task<List<SMSCampus>> GetAllAsync();
        Task<SMSCampus?> GetByIdAsync(int id);
        Task<ResponseDto?> DeleteByIdAsync(int id);
    }
}

using School.API.Data.DBModels.Academic;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;

namespace School.API.Repositories.Interfaces.Academic
{
    public interface ISMSCampusRepository
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

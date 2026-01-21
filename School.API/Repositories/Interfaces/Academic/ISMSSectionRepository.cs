using School.API.Data.DBModels.Academic;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;

namespace School.API.Repositories.Interfaces.Academic
{
    public interface ISMSSectionRepository
    {
        Task<ResponseDto> SaveAsync(
            SMSSectionSaveDto dto,
            int userId,
            string userIp);

        Task<List<SMSSectionSaveDto>> GetAllAsync();
        Task<SMSSectionSaveDto?> GetByIdAsync(int id);
        Task<ResponseDto?> DeleteByIdAsync(int id);
    }
}

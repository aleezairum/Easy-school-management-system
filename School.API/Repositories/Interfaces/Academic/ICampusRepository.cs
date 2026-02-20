using School.API.Data.DBModels.Academic;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;

namespace School.API.Repositories.Interfaces.Academic
{
    public interface ICampusRepository
    {
        Task<ResponseDto> SaveAsync(
            CampusSaveDto dto,
            int userId,
            string userIp);

        Task<List<Campus>> GetAllAsync();
        Task<Campus?> GetByIdAsync(int id);
        Task<ResponseDto?> DeleteByIdAsync(int id);
    }
}

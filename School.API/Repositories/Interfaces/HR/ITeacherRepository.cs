using School.API.Data.DBModels.HR;
using School.API.DTOs;
using School.API.DTOs.Common;

namespace School.API.Repositories.Interfaces.HR
{
    public interface ITeacherRepository
    {
        Task<ResponseDto> SaveAsync(
            TeacherDto dto,
            int userId,
            string userIp);

        Task<List<Teacher>> GetAllAsync();
        Task<Teacher?> GetByIdAsync(int id);
        Task<ResponseDto?> DeleteByIdAsync(int id);
    }
}

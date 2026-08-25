using School.API.Data.DBModels.HR;
using School.API.DTOs;
using School.API.DTOs.Common;

namespace School.API.Services.Interfaces.HR
{
    public interface ITeacherService
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

using School.API.DTOs.Academic;
using School.API.DTOs.Common;

namespace School.API.Services.Interfaces.Academic
{
    public interface IStudentService
    {
        Task<ResponseDto> SaveAsync(StudentSaveDto dto, int userId, string userIp);
        Task<List<StudentSaveDto>> GetAllAsync();
        Task<StudentSaveDto?> GetByIdAsync(int id);
        Task<ResponseDto?> DeleteByIdAsync(int id);
    }
}

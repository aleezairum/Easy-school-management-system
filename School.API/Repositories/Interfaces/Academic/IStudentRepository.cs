using School.API.DTOs.Academic;
using School.API.DTOs.Common;

namespace School.API.Repositories.Interfaces.Academic
{
    public interface IStudentRepository
    {
        Task<ResponseDto> SaveAsync(StudentSaveDto dto, int userId, string userIp);
        Task<List<StudentSaveDto>> GetAllAsync();
        Task<StudentSaveDto?> GetByIdAsync(int id);
        Task<ResponseDto?> DeleteByIdAsync(int id);
        Task<ResponseDto> ToggleStatusAsync(int vid, int userId, string userIp);
    }
}

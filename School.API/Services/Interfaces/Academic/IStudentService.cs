using School.API.DTOs.Academic;
using School.API.DTOs.Common;

namespace School.API.Services.Interfaces.Academic
{
    public interface IStudentService
    {
        Task<ResponseDto> SaveAsync(StudentSaveDto dto, int userId, string userIp);
        Task<ResponseDto> StatusChangeAsync(string StudentIDs, int StatusID, int userId, string userIp);
        Task<ResponseDto> SectionChangeAsync(string StudentIDs, int StatusID, int userId, string userIp);
        Task<List<StudentSaveDto>> GetAllAsync();
        Task<StudentSaveDto?> GetByIdAsync(int id);
        Task<ResponseDto?> DeleteByIdAsync(int id);
        Task<ResponseDto> ToggleStatusAsync(int vid, int userId, string userIp);
    }
}

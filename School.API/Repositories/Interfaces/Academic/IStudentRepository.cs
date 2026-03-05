using School.API.DTOs.Academic;
using School.API.DTOs.Common;

namespace School.API.Repositories.Interfaces.Academic
{
    public interface IStudentRepository
    {
        Task<ResponseDto> SaveAsync(StudentSaveDto dto, int userId, string userIp);
        Task<ResponseDto> StatusChangeAsync(string StudentIDs, int StatusID, int userId, string userIp);
        Task<ResponseDto> GradeChangeAsync(string StudentIDs, int GradeID, string GradeChangeDescription, int userId, string userIp);
        Task<ResponseDto> FeeChangeAsync(string StudentIDs, Decimal Fee, string FeeChangeDescription, int userId, string userIp);
        Task<ResponseDto> SectionChangeAsync(string StudentIDs, int StatusID, int userId, string userIp);
        Task<List<StudentSaveDto>> GetAllAsync();
        Task<StudentSaveDto?> GetByIdAsync(int id);
        Task<ResponseDto?> DeleteByIdAsync(int id);
        Task<ResponseDto> ToggleStatusAsync(int vid, int userId, string userIp);
        Task<ResponseDto> AvailAcademyAsync(int vid, bool IsAvailAcademy, int userId, string userIp);
    }
}

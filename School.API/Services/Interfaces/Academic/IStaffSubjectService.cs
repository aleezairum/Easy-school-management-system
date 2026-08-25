using School.API.DTOs.Academic;
using School.API.DTOs.Common;

namespace School.API.Services.Interfaces.Academic
{
    public interface IStaffSubjectService
    {
        Task<List<StaffSubjectRowDto>> GetAsync(int teacherId, int classId, int sectionId);
        Task<ResponseDto> SaveAsync(StaffSubjectDto dto, int userId, string userIp);
        Task<ResponseDto?> DeleteByIdAsync(int id);
    }
}

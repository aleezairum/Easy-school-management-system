using School.API.DTOs.Academic;
using School.API.DTOs.Common;

namespace School.API.Services.Interfaces.Academic
{
    public interface IStaffSubjectService
    {
        Task<List<StaffSubjectRowDto>> GetAsync(int teacherId, int classId, int sectionId, int examId);
        Task<ResponseDto> SaveAsync(StaffSubjectDto dto, int userId, string userIp);
        Task<ResponseDto?> DeleteByIdAsync(int id);
        Task<ResponseDto?> CopyAsync(CopyStaffSubjectDto dto, int userId, string userIp);
    }
}

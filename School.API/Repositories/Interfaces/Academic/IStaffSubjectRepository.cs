using School.API.DTOs.Academic;
using School.API.DTOs.Common;

namespace School.API.Repositories.Interfaces.Academic
{
    public interface IStaffSubjectRepository
    {
        Task<List<StaffSubjectRowDto>> GetAsync(int teacherId, int classId, int sectionId);
        Task<ResponseDto> SaveAsync(StaffSubjectDto dto, int userId, string userIp);
        Task<ResponseDto?> DeleteByIdAsync(int id);
    }
}

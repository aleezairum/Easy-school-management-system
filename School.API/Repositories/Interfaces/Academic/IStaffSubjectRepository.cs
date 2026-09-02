using School.API.DTOs.Academic;
using School.API.DTOs.Common;

namespace School.API.Repositories.Interfaces.Academic
{
    public interface IStaffSubjectRepository
    {
        Task<List<StaffSubjectRowDto>> GetAsync(int teacherId, int classId, int sectionId, int examId);
        Task<ResponseDto> SaveAsync(StaffSubjectDto dto, int userId, string userIp);
        Task<ResponseDto?> DeleteByIdAsync(int id);

        // "Copy from previous exam" — bulk-carries one exam's assignments into
        // another, skipping what the target already has.
        Task<ResponseDto?> CopyAsync(CopyStaffSubjectDto dto, int userId, string userIp);
    }
}

using School.API.DTOs.Academic;
using School.API.DTOs.Common;

namespace School.API.Repositories.Interfaces.Academic
{
    // Academy subject allocation (Scripts/AcademyStaffSubject.sql). Same rows
    // as Subject Allocation; SectionId 0 means all sections of the class.
    public interface IAcademyStaffSubjectRepository
    {
        Task<List<StaffSubjectRowDto>> GetAsync(int teacherId, int classId, int sectionId, int examId);
        Task<ResponseDto?> SaveAsync(StaffSubjectDto dto, int userId, string userIp);
        Task<ResponseDto?> DeleteByIdAsync(int id);
        Task<ResponseDto?> CopyAsync(CopyStaffSubjectDto dto, int userId, string userIp);
    }
}

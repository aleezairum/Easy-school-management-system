using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.Repositories.Interfaces.Academic;
using School.API.Services.Interfaces.Academic;

namespace School.API.Services.Implementations.Academic
{
    public class AcademyStaffSubjectService : IAcademyStaffSubjectService
    {
        private readonly IAcademyStaffSubjectRepository _repo;

        public AcademyStaffSubjectService(IAcademyStaffSubjectRepository repo)
        {
            _repo = repo;
        }

        public Task<List<StaffSubjectRowDto>> GetAsync(int teacherId, int classId, int sectionId, int examId)
            => _repo.GetAsync(teacherId, classId, sectionId, examId);

        public Task<ResponseDto?> SaveAsync(StaffSubjectDto dto, int userId, string userIp)
            => _repo.SaveAsync(dto, userId, userIp);

        public Task<ResponseDto?> DeleteByIdAsync(int id)
            => _repo.DeleteByIdAsync(id);

        public Task<ResponseDto?> CopyAsync(CopyStaffSubjectDto dto, int userId, string userIp)
            => _repo.CopyAsync(dto, userId, userIp);
    }
}

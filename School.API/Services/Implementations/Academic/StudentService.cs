using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.Repositories.Interfaces.Academic;
using School.API.Services.Interfaces.Academic;

namespace School.API.Services.Implementations.Academic
{
    public class StudentService : IStudentService
    {
        private readonly IStudentRepository _repo;

        public StudentService(IStudentRepository repo)
        {
            _repo = repo;
        }

        public Task<ResponseDto> SaveAsync(StudentSaveDto dto, int userId, string userIp)
            => _repo.SaveAsync(dto, userId, userIp);
        public Task<ResponseDto> StatusChangeAsync(string StudentIDs, int StatusID, int userId, string userIp)
            => _repo.StatusChangeAsync(StudentIDs, StatusID, userId, userIp);
        public Task<ResponseDto> SectionChangeAsync(string StudentIDs, int StatusID, int userId, string userIp)
            => _repo.SectionChangeAsync(StudentIDs, StatusID, userId, userIp);
        public Task<ResponseDto> GradeChangeAsync(string StudentIDs, int GradeID, string GradeChangeDescription, int userId, string userIp)
            => _repo.GradeChangeAsync(StudentIDs, GradeID, GradeChangeDescription, userId, userIp);
        public Task<ResponseDto> FeeChangeAsync(string StudentIDs, Decimal Fee, string FeeChangeDescription, int userId, string userIp)
            => _repo.FeeChangeAsync(StudentIDs, Fee, FeeChangeDescription, userId, userIp);

        public Task<List<StudentSaveDto>> GetAllAsync()
            => _repo.GetAllAsync();

        public Task<StudentSaveDto?> GetByIdAsync(int id)
            => _repo.GetByIdAsync(id);

        public Task<ResponseDto?> DeleteByIdAsync(int id)
            => _repo.DeleteByIdAsync(id);

        public Task<ResponseDto> ToggleStatusAsync(int vid, int userId, string userIp)
            => _repo.ToggleStatusAsync(vid, userId, userIp);
        public Task<ResponseDto> AvailAcademyAsync(int vid, bool IsAvailAcademy, int userId, string userIp)
            => _repo.AvailAcademyAsync(vid, IsAvailAcademy, userId, userIp);

        public Task<List<StudentComboDto>> GetStudentsForComboAsync(int classId, int sectionId)
            => _repo.GetStudentsForComboAsync(classId, sectionId); 

    }
}

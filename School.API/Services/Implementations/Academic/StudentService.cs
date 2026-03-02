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

        public Task<List<StudentSaveDto>> GetAllAsync()
            => _repo.GetAllAsync();

        public Task<StudentSaveDto?> GetByIdAsync(int id)
            => _repo.GetByIdAsync(id);

        public Task<ResponseDto?> DeleteByIdAsync(int id)
            => _repo.DeleteByIdAsync(id);

        public Task<ResponseDto> ToggleStatusAsync(int vid, int userId, string userIp)
            => _repo.ToggleStatusAsync(vid, userId, userIp);
    }
}

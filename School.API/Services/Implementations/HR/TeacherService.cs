using School.API.Data.DBModels.HR;
using School.API.DTOs;
using School.API.DTOs.Common;
using School.API.Repositories.Interfaces.HR;
using School.API.Services.Interfaces.HR;

namespace School.API.Services.Implementations.HR
{
    public class TeacherService : ITeacherService
    {
        private readonly ITeacherRepository _repo;

        public TeacherService(ITeacherRepository repo)
        {
            _repo = repo;
        }

        public Task<ResponseDto> SaveAsync(
            TeacherDto dto,
            int userId,
            string userIp)
            => _repo.SaveAsync(dto, userId, userIp);

        public Task<List<Teacher>> GetAllAsync()
            => _repo.GetAllAsync();

        public Task<Teacher?> GetByIdAsync(int id)
            => _repo.GetByIdAsync(id);

        public Task<ResponseDto?> DeleteByIdAsync(int id)
           => _repo.DeleteByIdAsync(id);
    }
}

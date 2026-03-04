using School.API.Data.DBModels.Student;
using School.API.DTOs.Common;
using School.API.DTOs.Student;
using School.API.Repositories.Interfaces.Student;
using School.API.Services.Interfaces.Student;

namespace School.API.Services.Implementations.Student
{
    public class StdAttendenceService : IStdAttendenceService
    {
        private readonly IStdAttendenceRepository _repo;

        public StdAttendenceService(IStdAttendenceRepository repo)
        {
            _repo = repo;
        }

        public Task<ResponseDto> SaveAsync(StdAttendenceSaveDto dto, int userId, string userIp)
            => _repo.SaveAsync(dto, userId, userIp);

        public Task<List<StdAttendence>> GetAllAsync(StdAttendenceFilterDto filter)
            => _repo.GetAllAsync(filter);

        public Task<StdAttendence?> GetByIdAsync(int id)
            => _repo.GetByIdAsync(id);

        public Task<ResponseDto?> DeleteByIdAsync(int id, int userId, string userIp)
            => _repo.DeleteByIdAsync(id, userId, userIp);
    }
}
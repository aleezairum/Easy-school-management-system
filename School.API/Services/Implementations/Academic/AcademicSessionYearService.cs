using School.API.Data.DBModels.Academic;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.Repositories.Interfaces.Academic;
using School.API.Services.Interfaces.Academic;

namespace School.API.Services.Implementations.Academic
{
    public class AcademicSessionYearService : IAcademicSessionYearService
    {
        private readonly IAcademicSessionYearRepository _repo;

        public AcademicSessionYearService(IAcademicSessionYearRepository repo)
        {
            _repo = repo;
        }

        public Task<ResponseDto> SaveAsync(
            AcademicSessionYearSaveDto dto,
            int userId,
            string userIp)
            => _repo.SaveAsync(dto, userId, userIp);

<<<<<<< HEAD
        public Task<List<AcademicSessionYearSaveDto>> GetAllAsync()
            => _repo.GetAllAsync();

        public Task<AcademicSessionYearSaveDto?> GetByIdAsync(int id)
=======
        public Task<List<AcademicSessionYear>> GetAllAsync()
            => _repo.GetAllAsync();

        public Task<AcademicSessionYear?> GetByIdAsync(int id)
>>>>>>> ddd2cfec04642aebc056f91a2df1715e14979d68
            => _repo.GetByIdAsync(id);

        public Task<ResponseDto?> DeleteByIdAsync(int id)
           => _repo.DeleteByIdAsync(id);
    }
}

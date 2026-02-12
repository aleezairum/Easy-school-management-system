using School.API.Data.DBModels.Academic;
using School.API.DTOs;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.Repositories.Interfaces.Academic;
using School.API.Services.Interfaces.Academic;

namespace School.API.Services.Implementations.Academic
{
    public class AcademicGradeService : IAcademicGradeService
    {
        private readonly IAcademicGradeRepository _repo;

        public AcademicGradeService(IAcademicGradeRepository repo)
        {
            _repo = repo;
        }

        public Task<ResponseDto> SaveAsync(
            AcademicGradeSaveDto dto,
            int userId,
            string userIp)
            => _repo.SaveAsync(dto, userId, userIp);

        public Task<List<AcademicGrades>> GetAllAsync()
            => _repo.GetAllAsync();

        public Task<AcademicGrades?> GetByIdAsync(int id)
            => _repo.GetByIdAsync(id);

        public Task<ResponseDto?> DeleteByIdAsync(int id)
           => _repo.DeleteByIdAsync(id);

        
    }
}

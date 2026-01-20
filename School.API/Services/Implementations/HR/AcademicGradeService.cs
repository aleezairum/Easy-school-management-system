using School.API.DTOs;
using School.API.Repositories.Interfaces.HR;
using School.API.Services.Interfaces.HR;

namespace School.API.Services.Implementations.HR
{
    public class AcademicGradeService : IAcademicGradeService
    {
        private readonly IAcademicGradeRepository _repository;

        public AcademicGradeService(IAcademicGradeRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<AcademicGradeListDto>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<AcademicGradeDto?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<AcademicGradeDto> CreateAsync(CreateAcademicGradeDto dto)
        {
            var entity = await _repository.CreateAsync(dto);
            return new AcademicGradeDto
            {
                Id = entity.Id,
                Code = entity.Code,
                Name = entity.Name,
                Description = entity.Description,
                Qualification = entity.Qualification,
                SortOrder = entity.SortOrder,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        public async Task<AcademicGradeDto?> UpdateAsync(int id, UpdateAcademicGradeDto dto)
        {
            var entity = await _repository.UpdateAsync(id, dto);
            if (entity == null) return null;

            return new AcademicGradeDto
            {
                Id = entity.Id,
                Code = entity.Code,
                Name = entity.Name,
                Description = entity.Description,
                Qualification = entity.Qualification,
                SortOrder = entity.SortOrder,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        public async Task<IEnumerable<AcademicGradeDropdownDto>> GetDropdownAsync()
        {
            return await _repository.GetDropdownAsync();
        }
    }
}

using School.API.DTOs;
using School.API.Repositories.Interfaces.HR;
using School.API.Services.Interfaces.HR;

namespace School.API.Services.Implementations.HR
{
    public class HRGradeService : IHRGradeService
    {
        private readonly IHRGradeRepository _repository;

        public HRGradeService(IHRGradeRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<HRGradeListDto>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<HRGradeDto?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<HRGradeDto> CreateAsync(CreateHRGradeDto dto)
        {
            var entity = await _repository.CreateAsync(dto);
            return new HRGradeDto
            {
                Id = entity.Id,
                Code = entity.Code,
                Name = entity.Name,
                Description = entity.Description,
                MinSalary = entity.MinSalary,
                MaxSalary = entity.MaxSalary,
                SortOrder = entity.SortOrder,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        public async Task<HRGradeDto?> UpdateAsync(int id, UpdateHRGradeDto dto)
        {
            var entity = await _repository.UpdateAsync(id, dto);
            if (entity == null) return null;

            return new HRGradeDto
            {
                Id = entity.Id,
                Code = entity.Code,
                Name = entity.Name,
                Description = entity.Description,
                MinSalary = entity.MinSalary,
                MaxSalary = entity.MaxSalary,
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

        public async Task<IEnumerable<HRGradeDropdownDto>> GetDropdownAsync()
        {
            return await _repository.GetDropdownAsync();
        }
    }
}

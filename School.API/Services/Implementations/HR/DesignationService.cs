using School.API.DTOs;
using School.API.Repositories.Interfaces.HR;
using School.API.Services.Interfaces.HR;

namespace School.API.Services.Implementations.HR
{
    public class DesignationService : IDesignationService
    {
        private readonly IDesignationRepository _repository;

        public DesignationService(IDesignationRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<DesignationListDto>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<DesignationDto?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<DesignationDto> CreateAsync(CreateDesignationDto dto)
        {
            var entity = await _repository.CreateAsync(dto);
            return new DesignationDto
            {
                Id = entity.Id,
                Code = entity.Code,
                Name = entity.Name,
                Description = entity.Description,
                SortOrder = entity.SortOrder,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        public async Task<DesignationDto?> UpdateAsync(int id, UpdateDesignationDto dto)
        {
            var entity = await _repository.UpdateAsync(id, dto);
            if (entity == null) return null;

            return new DesignationDto
            {
                Id = entity.Id,
                Code = entity.Code,
                Name = entity.Name,
                Description = entity.Description,
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

        public async Task<IEnumerable<DesignationDropdownDto>> GetDropdownAsync()
        {
            return await _repository.GetDropdownAsync();
        }
    }
}

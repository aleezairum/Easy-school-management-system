using School.API.Data.DBModels.HR;
using School.API.DTOs;
using School.API.Repositories.Interfaces.HR;
using School.API.Services.Interfaces.HR;

namespace School.API.Services.Implementations.HR
{
    public class StaffCategoryService : IStaffCategoryService
    {
        private readonly IStaffCategoryRepository _repository;

        public StaffCategoryService(IStaffCategoryRepository repository)
        {
            _repository = repository;
        }

        public Task<IEnumerable<StaffCategoryListDto>> GetAllAsync() => _repository.GetAllAsync();

        public Task<StaffCategoryDto?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);

        public async Task<StaffCategoryDto> CreateAsync(CreateStaffCategoryDto dto)
            => ToDto(await _repository.CreateAsync(dto));

        public async Task<StaffCategoryDto?> UpdateAsync(int id, UpdateStaffCategoryDto dto)
        {
            var entity = await _repository.UpdateAsync(id, dto);
            return entity == null ? null : ToDto(entity);
        }

        public Task<bool> DeleteAsync(int id) => _repository.DeleteAsync(id);

        public Task<IEnumerable<StaffCategoryDropdownDto>> GetDropdownAsync() => _repository.GetDropdownAsync();

        public Task<bool> CodeExistsAsync(string code, int? excludeId = null)
            => _repository.CodeExistsAsync(code, excludeId);

        public Task<bool> IsInUseAsync(int id) => _repository.IsInUseAsync(id);

        private static StaffCategoryDto ToDto(HrStaffCategory c) => new()
        {
            Id = c.Id,
            Code = c.Code,
            Name = c.Name,
            Description = c.Description,
            SortOrder = c.SortOrder,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        };
    }
}

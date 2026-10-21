using School.API.DTOs;

namespace School.API.Services.Interfaces.HR
{
    public interface IStaffCategoryService
    {
        Task<IEnumerable<StaffCategoryListDto>> GetAllAsync();
        Task<StaffCategoryDto?> GetByIdAsync(int id);
        Task<StaffCategoryDto> CreateAsync(CreateStaffCategoryDto dto);
        Task<StaffCategoryDto?> UpdateAsync(int id, UpdateStaffCategoryDto dto);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<StaffCategoryDropdownDto>> GetDropdownAsync();
        Task<bool> CodeExistsAsync(string code, int? excludeId = null);
        Task<bool> IsInUseAsync(int id);
    }
}

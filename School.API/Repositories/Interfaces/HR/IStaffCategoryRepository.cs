using School.API.Data.DBModels.HR;
using School.API.DTOs;

namespace School.API.Repositories.Interfaces.HR
{
    public interface IStaffCategoryRepository
    {
        Task<IEnumerable<StaffCategoryListDto>> GetAllAsync();
        Task<StaffCategoryDto?> GetByIdAsync(int id);
        Task<HrStaffCategory> CreateAsync(CreateStaffCategoryDto dto);
        Task<HrStaffCategory?> UpdateAsync(int id, UpdateStaffCategoryDto dto);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<StaffCategoryDropdownDto>> GetDropdownAsync();
        Task<bool> CodeExistsAsync(string code, int? excludeId = null);
        Task<bool> IsInUseAsync(int id);
    }
}

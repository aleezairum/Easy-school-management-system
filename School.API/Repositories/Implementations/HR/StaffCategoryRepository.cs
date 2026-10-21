using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.HR;
using School.API.DTOs;
using School.API.Repositories.Interfaces.HR;

namespace School.API.Repositories.Implementations.HR
{
    /// <summary>
    /// The staff category list — see Scripts/StaffCategory.sql for what a
    /// category is and why it is not a designation.
    /// </summary>
    public class StaffCategoryRepository : IStaffCategoryRepository
    {
        private readonly SchoolDbContext _context;

        public StaffCategoryRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<StaffCategoryListDto>> GetAllAsync()
        {
            return await _context.StaffCategories
                .OrderBy(c => c.SortOrder ?? 999)
                .ThenBy(c => c.Name)
                .Select(c => new StaffCategoryListDto
                {
                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name,
                    Description = c.Description,
                    SortOrder = c.SortOrder,
                    IsActive = c.IsActive,
                    EmployeeCount = _context.Employees.Count(e => e.StaffCategoryId == c.Id)
                })
                .ToListAsync();
        }

        public async Task<StaffCategoryDto?> GetByIdAsync(int id)
        {
            return await _context.StaffCategories
                .Where(c => c.Id == id)
                .Select(c => new StaffCategoryDto
                {
                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name,
                    Description = c.Description,
                    SortOrder = c.SortOrder,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<HrStaffCategory> CreateAsync(CreateStaffCategoryDto dto)
        {
            var category = new HrStaffCategory
            {
                Code = dto.Code.Trim(),
                Name = dto.Name.Trim(),
                Description = dto.Description,
                SortOrder = dto.SortOrder,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.StaffCategories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<HrStaffCategory?> UpdateAsync(int id, UpdateStaffCategoryDto dto)
        {
            var category = await _context.StaffCategories.FindAsync(id);
            if (category == null) return null;

            category.Code = dto.Code.Trim();
            category.Name = dto.Name.Trim();
            category.Description = dto.Description;
            category.SortOrder = dto.SortOrder;
            category.IsActive = dto.IsActive;
            category.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var category = await _context.StaffCategories.FindAsync(id);
            if (category == null) return false;

            _context.StaffCategories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<StaffCategoryDropdownDto>> GetDropdownAsync()
        {
            return await _context.StaffCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder ?? 999)
                .ThenBy(c => c.Name)
                .Select(c => new StaffCategoryDropdownDto
                {
                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name
                })
                .ToListAsync();
        }

        public async Task<bool> CodeExistsAsync(string code, int? excludeId = null)
        {
            return await _context.StaffCategories
                .AnyAsync(c => c.Code.ToLower() == code.ToLower()
                               && (!excludeId.HasValue || c.Id != excludeId.Value));
        }

        public async Task<bool> IsInUseAsync(int id)
        {
            return await _context.Employees.AnyAsync(e => e.StaffCategoryId == id);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.HR;
using School.API.DTOs;
using School.API.Repositories.Interfaces.HR;

namespace School.API.Repositories.Implementations.HR
{
    public class DesignationRepository : IDesignationRepository
    {
        private readonly SchoolDbContext _context;

        public DesignationRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DesignationListDto>> GetAllAsync()
        {
            return await _context.Designations
                .OrderBy(d => d.SortOrder ?? 999)
                .ThenBy(d => d.Name)
                .Select(d => new DesignationListDto
                {
                    Id = d.Id,
                    Code = d.Code,
                    Name = d.Name,
                    Description = d.Description,
                    SortOrder = d.SortOrder,
                    IsActive = d.IsActive,
                    EmployeeCount = d.Employees != null ? d.Employees.Count : 0
                })
                .ToListAsync();
        }

        public async Task<DesignationDto?> GetByIdAsync(int id)
        {
            return await _context.Designations
                .Where(d => d.Id == id)
                .Select(d => new DesignationDto
                {
                    Id = d.Id,
                    Code = d.Code,
                    Name = d.Name,
                    Description = d.Description,
                    SortOrder = d.SortOrder,
                    IsActive = d.IsActive,
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<Designation> CreateAsync(CreateDesignationDto dto)
        {
            var designation = new Designation
            {
                Code = dto.Code,
                Name = dto.Name,
                Description = dto.Description,
                SortOrder = dto.SortOrder,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Designations.Add(designation);
            await _context.SaveChangesAsync();
            return designation;
        }

        public async Task<Designation?> UpdateAsync(int id, UpdateDesignationDto dto)
        {
            var designation = await _context.Designations.FindAsync(id);
            if (designation == null) return null;

            designation.Code = dto.Code;
            designation.Name = dto.Name;
            designation.Description = dto.Description;
            designation.SortOrder = dto.SortOrder;
            designation.IsActive = dto.IsActive;
            designation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return designation;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var designation = await _context.Designations.FindAsync(id);
            if (designation == null) return false;

            _context.Designations.Remove(designation);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<DesignationDropdownDto>> GetDropdownAsync()
        {
            return await _context.Designations
                .Where(d => d.IsActive)
                .OrderBy(d => d.SortOrder ?? 999)
                .ThenBy(d => d.Name)
                .Select(d => new DesignationDropdownDto
                {
                    Id = d.Id,
                    Code = d.Code,
                    Name = d.Name
                })
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Designations.AnyAsync(d => d.Id == id);
        }

        public async Task<bool> NameExistsAsync(string name, int? excludeId = null)
        {
            return await _context.Designations
                .AnyAsync(d => d.Name.ToLower() == name.ToLower() && (!excludeId.HasValue || d.Id != excludeId.Value));
        }
    }
}

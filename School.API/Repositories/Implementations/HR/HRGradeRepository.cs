using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.HR;
using School.API.DTOs;
using School.API.Repositories.Interfaces.HR;

namespace School.API.Repositories.Implementations.HR
{
    public class HRGradeRepository : IHRGradeRepository
    {
        private readonly SchoolDbContext _context;

        public HRGradeRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<HRGradeListDto>> GetAllAsync()
        {
            return await _context.HRGrades
                .OrderBy(h => h.SortOrder ?? 999)
                .ThenBy(h => h.Code)
                .Select(h => new HRGradeListDto
                {
                    Id = h.Id,
                    Code = h.Code,
                    Name = h.Name,
                    MinSalary = h.MinSalary,
                    MaxSalary = h.MaxSalary,
                    SortOrder = h.SortOrder,
                    IsActive = h.IsActive,
                    EmployeeCount = h.Employees != null ? h.Employees.Count : 0
                })
                .ToListAsync();
        }

        public async Task<HRGradeDto?> GetByIdAsync(int id)
        {
            return await _context.HRGrades
                .Where(h => h.Id == id)
                .Select(h => new HRGradeDto
                {
                    Id = h.Id,
                    Code = h.Code,
                    Name = h.Name,
                    Description = h.Description,
                    MinSalary = h.MinSalary,
                    MaxSalary = h.MaxSalary,
                    SortOrder = h.SortOrder,
                    IsActive = h.IsActive,
                    CreatedAt = h.CreatedAt,
                    UpdatedAt = h.UpdatedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<HRGrade> CreateAsync(CreateHRGradeDto dto)
        {
            var hrGrade = new HRGrade
            {
                Code = dto.Code,
                Name = dto.Name,
                Description = dto.Description,
                MinSalary = dto.MinSalary,
                MaxSalary = dto.MaxSalary,
                SortOrder = dto.SortOrder,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.HRGrades.Add(hrGrade);
            await _context.SaveChangesAsync();
            return hrGrade;
        }

        public async Task<HRGrade?> UpdateAsync(int id, UpdateHRGradeDto dto)
        {
            var hrGrade = await _context.HRGrades.FindAsync(id);
            if (hrGrade == null) return null;

            hrGrade.Code = dto.Code;
            hrGrade.Name = dto.Name;
            hrGrade.Description = dto.Description;
            hrGrade.MinSalary = dto.MinSalary;
            hrGrade.MaxSalary = dto.MaxSalary;
            hrGrade.SortOrder = dto.SortOrder;
            hrGrade.IsActive = dto.IsActive;
            hrGrade.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return hrGrade;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var hrGrade = await _context.HRGrades.FindAsync(id);
            if (hrGrade == null) return false;

            _context.HRGrades.Remove(hrGrade);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<HRGradeDropdownDto>> GetDropdownAsync()
        {
            return await _context.HRGrades
                .Where(h => h.IsActive)
                .OrderBy(h => h.SortOrder ?? 999)
                .ThenBy(h => h.Code)
                .Select(h => new HRGradeDropdownDto
                {
                    Id = h.Id,
                    Code = h.Code,
                    Name = h.Name
                })
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.HRGrades.AnyAsync(h => h.Id == id);
        }

        public async Task<bool> CodeExistsAsync(string code, int? excludeId = null)
        {
            return await _context.HRGrades
                .AnyAsync(h => h.Code.ToLower() == code.ToLower() && (!excludeId.HasValue || h.Id != excludeId.Value));
        }
    }
}

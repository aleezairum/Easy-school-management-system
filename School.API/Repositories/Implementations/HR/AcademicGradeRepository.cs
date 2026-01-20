using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.HR;
using School.API.DTOs;
using School.API.Repositories.Interfaces.HR;

namespace School.API.Repositories.Implementations.HR
{
    public class AcademicGradeRepository : IAcademicGradeRepository
    {
        private readonly SchoolDbContext _context;

        public AcademicGradeRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AcademicGradeListDto>> GetAllAsync()
        {
            return await _context.AcademicGrades
                .OrderBy(a => a.SortOrder ?? 999)
                .ThenBy(a => a.Code)
                .Select(a => new AcademicGradeListDto
                {
                    Id = a.Id,
                    Code = a.Code,
                    Name = a.Name,
                    Qualification = a.Qualification,
                    SortOrder = a.SortOrder,
                    IsActive = a.IsActive,
                    EmployeeCount = a.Employees != null ? a.Employees.Count : 0
                })
                .ToListAsync();
        }

        public async Task<AcademicGradeDto?> GetByIdAsync(int id)
        {
            return await _context.AcademicGrades
                .Where(a => a.Id == id)
                .Select(a => new AcademicGradeDto
                {
                    Id = a.Id,
                    Code = a.Code,
                    Name = a.Name,
                    Description = a.Description,
                    Qualification = a.Qualification,
                    SortOrder = a.SortOrder,
                    IsActive = a.IsActive,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<AcademicGrade> CreateAsync(CreateAcademicGradeDto dto)
        {
            var academicGrade = new AcademicGrade
            {
                Code = dto.Code,
                Name = dto.Name,
                Description = dto.Description,
                Qualification = dto.Qualification,
                SortOrder = dto.SortOrder,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.AcademicGrades.Add(academicGrade);
            await _context.SaveChangesAsync();
            return academicGrade;
        }

        public async Task<AcademicGrade?> UpdateAsync(int id, UpdateAcademicGradeDto dto)
        {
            var academicGrade = await _context.AcademicGrades.FindAsync(id);
            if (academicGrade == null) return null;

            academicGrade.Code = dto.Code;
            academicGrade.Name = dto.Name;
            academicGrade.Description = dto.Description;
            academicGrade.Qualification = dto.Qualification;
            academicGrade.SortOrder = dto.SortOrder;
            academicGrade.IsActive = dto.IsActive;
            academicGrade.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return academicGrade;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var academicGrade = await _context.AcademicGrades.FindAsync(id);
            if (academicGrade == null) return false;

            _context.AcademicGrades.Remove(academicGrade);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<AcademicGradeDropdownDto>> GetDropdownAsync()
        {
            return await _context.AcademicGrades
                .Where(a => a.IsActive)
                .OrderBy(a => a.SortOrder ?? 999)
                .ThenBy(a => a.Code)
                .Select(a => new AcademicGradeDropdownDto
                {
                    Id = a.Id,
                    Code = a.Code,
                    Name = a.Name
                })
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.AcademicGrades.AnyAsync(a => a.Id == id);
        }

        public async Task<bool> CodeExistsAsync(string code, int? excludeId = null)
        {
            return await _context.AcademicGrades
                .AnyAsync(a => a.Code.ToLower() == code.ToLower() && (!excludeId.HasValue || a.Id != excludeId.Value));
        }
    }
}

using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.HR;
using School.API.DTOs;
using School.API.DTOs.Common;
using School.API.Repositories.Interfaces.HR;

namespace School.API.Repositories.Implementations.HR
{
    public class TeacherRepository : ITeacherRepository
    {
        private readonly SchoolDbContext _context;

        public TeacherRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<List<Teacher>> GetAllAsync()
        {
            var resultList = await _context
                .Set<Teacher>()
                .FromSqlRaw("EXEC SpGet_SMSTeacher @VID={0}", 0) // 0 to get all
                .AsNoTracking()
                .ToListAsync();

            return resultList;
        }

        public async Task<Teacher?> GetByIdAsync(int vid)
        {
            var resultList = await _context
                .Set<Teacher>()
                .FromSqlRaw("EXEC SpGet_SMSTeacher @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();

            return resultList.FirstOrDefault();
        }

        public async Task<ResponseDto?> DeleteByIdAsync(int vid)
        {
            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw("EXEC SpDelete_SMSTeacher @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();

            return resultList.FirstOrDefault();
        }

        public async Task<ResponseDto> SaveAsync(TeacherDto dto, int userId, string userIp)
        {
            // Execute the stored procedure
            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpSave_SMSTeacher @VID={0}, @VName={1}, @FatherName={2}, @Gender={3}, " +
                    "@CNIC={4}, @Mobile={5}, @Email={6}, @Address={7}, @Qualification={8}, " +
                    "@ExperienceYears={9}, @IsActive={10}, @UserID={11}, @UserIP={12}",
                    dto.VID,
                    dto.VName,
                    (object?)dto.FatherName ?? DBNull.Value,
                    dto.Gender ?? 0,
                    (object?)dto.CNIC ?? DBNull.Value,
                    (object?)dto.Mobile ?? DBNull.Value,
                    (object?)dto.Email ?? DBNull.Value,
                    (object?)dto.Address ?? DBNull.Value,
                    (object?)dto.Qualification ?? DBNull.Value,
                    (object?)dto.ExperienceYears ?? DBNull.Value,
                    dto.IsActive ?? true,
                    userId,
                    userIp)
                .AsNoTracking()
                .ToListAsync(); // materialize results in memory

            // Safely get the first (or single) record
            var result = resultList.FirstOrDefault();

            return result;
        }
    }
}

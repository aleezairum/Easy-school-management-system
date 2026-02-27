using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.Academic;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.Repositories.Interfaces.Academic;

namespace School.API.Repositories.Implementations.Academic
{
    public class StudentStatusRepository : IStudentStatusRepository
    {
        private readonly SchoolDbContext _context;

        public StudentStatusRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<List<StudentStatus>> GetAllAsync()
        {
            var resultList = await _context
                .Set<StudentStatus>()
                .FromSqlRaw("EXEC SpGet_SMSStudentStatus @VID={0}", 0)
                .AsNoTracking()
                .ToListAsync();
            return resultList;
        }

        public async Task<StudentStatus?> GetByIdAsync(int vid)
        {
            var resultList = await _context
                .Set<StudentStatus>()
                .FromSqlRaw("EXEC SpGet_SMSStudentStatus @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();
            return resultList.FirstOrDefault();
        }

        public async Task<ResponseDto?> DeleteByIdAsync(int vid)
        {
            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw("EXEC SpDelete_SMSStudentStatus @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();
            return resultList.FirstOrDefault();
        }

        public async Task<ResponseDto> SaveAsync(StudentStatusSaveDto dto, int userId, string userIp)
        {
            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpSave_SMSStudentStatus @VID={0}, @VName={1}, @IsActive={2}, @UserID={3}, @UserIP={4}",
                    dto.VID,
                    dto.VName,
                    dto.IsActive,
                    userId,
                    userIp)
                .AsNoTracking()
                .ToListAsync();
            return resultList.FirstOrDefault();
        }
    }
}

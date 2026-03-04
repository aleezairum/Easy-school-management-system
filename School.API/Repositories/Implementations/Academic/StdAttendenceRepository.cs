using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.Student;
using School.API.DTOs.Common;
using School.API.DTOs.Student;
using School.API.Repositories.Interfaces.Student;

namespace School.API.Repositories.Implementations.Student
{
    public class StdAttendenceRepository : IStdAttendenceRepository
    {
        private readonly SchoolDbContext _context;

        public StdAttendenceRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<List<StdAttendence>> GetAllAsync(StdAttendenceFilterDto filter)
        {
            return await _context
                .Set<StdAttendence>()
                .FromSqlRaw(
                    "EXEC SpGet_StdAttendence @VID={0}, @SessionID={1}, @StudentID={2}, @SectionID={3}, @DateFrom={4}, @DateTo={5}",
                    filter.VID ?? (object)DBNull.Value,
                    filter.SessionID ?? (object)DBNull.Value,
                    filter.StudentID ?? (object)DBNull.Value,
                    filter.SectionID ?? (object)DBNull.Value,
                    filter.DateFrom ?? (object)DBNull.Value,
                    filter.DateTo ?? (object)DBNull.Value)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<StdAttendence?> GetByIdAsync(int vid)
        {
            var result = await _context
                .Set<StdAttendence>()
                .FromSqlRaw(
                    "EXEC SpGet_StdAttendence @VID={0}, @SessionID={1}, @StudentID={2}, @SectionID={3}, @DateFrom={4}, @DateTo={5}",
                    vid,
                    (object)DBNull.Value,
                    (object)DBNull.Value,
                    (object)DBNull.Value,
                    (object)DBNull.Value,
                    (object)DBNull.Value)
                .AsNoTracking()
                .ToListAsync();

            return result.FirstOrDefault();
        }

        public async Task<ResponseDto?> DeleteByIdAsync(int vid, int userId, string userIp)
        {
            var result = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpDelete_StdAttendence @VID={0}, @UserID={1}, @UserIP={2}",
                    vid,
                    userId,
                    userIp)
                .AsNoTracking()
                .ToListAsync();

            return result.FirstOrDefault();
        }

        public async Task<ResponseDto> SaveAsync(StdAttendenceSaveDto dto, int userId, string userIp)
        {
            var result = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpSave_StdAttendence @VID={0}, @SessionID={1}, @StudentID={2}, @SectionID={3}, @VDate={4}, @Attendence={5}, @IsLate={6}, @IsAgain={7}, @Remarks={8}, @ISentSMS={9}, @UserID={10}, @UserIP={11}",
                    dto.VID,
                    dto.SessionID,
                    dto.StudentID,
                    dto.SectionID,
                    dto.VDate,
                    dto.Attendence,
                    dto.IsLate,
                    dto.IsAgain,
                    dto.Remarks ?? (object)DBNull.Value,
                    dto.ISentSMS,
                    userId,
                    userIp)
                .AsNoTracking()
                .ToListAsync();

            return result.FirstOrDefault();
        }
    }
}
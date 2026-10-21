using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.Repositories.Interfaces.Academic;

namespace School.API.Repositories.Implementations.Academic
{
    public class ClassTimingRepository : IClassTimingRepository
    {
        private readonly SchoolDbContext _context;

        public ClassTimingRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<List<ClassTimingDto>> GetAllAsync(int campusId = 0)
        {
            return await _context
                .Set<ClassTimingDto>()
                .FromSqlRaw("EXEC SpGet_SMSClassTiming @VID={0}, @CampusID={1}", 0, campusId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ClassTimingDto?> GetByIdAsync(int vid)
        {
            var rows = await _context
                .Set<ClassTimingDto>()
                .FromSqlRaw("EXEC SpGet_SMSClassTiming @VID={0}, @CampusID={1}", vid, 0)
                .AsNoTracking()
                .ToListAsync();

            return rows.FirstOrDefault();
        }

        public async Task<ResponseDto?> SaveAsync(SaveClassTimingDto dto, int userId, string userIp)
        {
            // The procedure upserts on (CampusID, ClassID) and does the time
            // validation, so there is nothing to check here — a bad row comes
            // back as ReturnCode -1 with the message to show the user.
            var rows = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpSave_SMSClassTiming @VID={0}, @CampusID={1}, @ClassID={2}, " +
                    "@TimeIn={3}, @TimeOut={4}, @FridayTimeIn={5}, @FridayTimeOut={6}, " +
                    "@AcademyTimeIn={7}, @AcademyTimeOut={8}, " +
                    "@IsActive={9}, @UserID={10}, @UserIP={11}",
                    dto.VID,
                    dto.CampusID,
                    dto.ClassID,
                    (object?)dto.TimeIn ?? DBNull.Value,
                    (object?)dto.TimeOut ?? DBNull.Value,
                    (object?)dto.FridayTimeIn ?? DBNull.Value,
                    (object?)dto.FridayTimeOut ?? DBNull.Value,
                    (object?)dto.AcademyTimeIn ?? DBNull.Value,
                    (object?)dto.AcademyTimeOut ?? DBNull.Value,
                    dto.IsActive,
                    userId,
                    (object?)userIp ?? DBNull.Value)
                .AsNoTracking()
                .ToListAsync();

            return rows.FirstOrDefault();
        }

        public async Task<ResponseDto?> DeleteByIdAsync(int vid)
        {
            var rows = await _context
                .Set<ResponseDto>()
                .FromSqlRaw("EXEC SpDelete_SMSClassTiming @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();

            return rows.FirstOrDefault();
        }
    }
}

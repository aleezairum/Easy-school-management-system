using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.Repositories.Interfaces.Academic;

namespace School.API.Repositories.Implementations.Academic
{
    public class SMSExamRepository : ISMSExamRepository
    {
        private readonly SchoolDbContext _context;

        public SMSExamRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<List<SMSExamSaveDto>> GetAllAsync()
        {
            return await _context
                .Set<SMSExamSaveDto>()
                .FromSqlRaw("EXEC SpGet_SMSExam @VID={0}", 0)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<SMSExamSaveDto?> GetByIdAsync(int vid)
        {
            var list = await _context
                .Set<SMSExamSaveDto>()
                .FromSqlRaw("EXEC SpGet_SMSExam @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();

            return list.FirstOrDefault();
        }

        public async Task<List<SMSExamSaveDto>> FillComboAsync()
        {
            return await _context
                .Set<SMSExamSaveDto>()
                .FromSqlRaw("EXEC SpFillCombo_SMSExam")
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ResponseDto?> DeleteByIdAsync(int vid)
        {
            var list = await _context
                .Set<ResponseDto>()
                .FromSqlRaw("EXEC SpDelete_SMSExam @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();

            return list.FirstOrDefault();
        }

        public async Task<ResponseDto> SaveAsync(SMSExamSaveDto dto, int userId, string userIp)
        {
            var list = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpSave_SMSExam " +
                    "@VID={0}, @VName={1}, @IsActive={2}, @UserID={3}, @UserIP={4}",
                    dto.VID,
                    (object?)dto.VName ?? DBNull.Value,
                    dto.IsActive ?? true,
                    userId,
                    userIp)
                .AsNoTracking()
                .ToListAsync();

            return list.FirstOrDefault();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.Repositories.Interfaces.Academic;

namespace School.API.Repositories.Implementations.Academic
{
    public class SMSSubjectRepository : ISMSSubjectRepository
    {
        private readonly SchoolDbContext _context;

        public SMSSubjectRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<List<SMSSubjectSaveDto>> GetAllAsync()
        {
            var resultList = await _context
                .Set<SMSSubjectSaveDto>()
                .FromSqlRaw("EXEC SpGet_SMSSubject @VID={0}", 0)
                .AsNoTracking()
                .ToListAsync();

            return resultList;
        }

        public async Task<SMSSubjectSaveDto?> GetByIdAsync(int vid)
        {
            var resultList = await _context
                .Set<SMSSubjectSaveDto>()
                .FromSqlRaw("EXEC SpGet_SMSSubject @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();

            return resultList.FirstOrDefault();
        }

        public async Task<List<SMSSubjectSaveDto>> FillComboAsync(int classId)
        {
            var resultList = await _context
                .Set<SMSSubjectSaveDto>()
                .FromSqlRaw("EXEC SpFillCombo_SMSSubject @ClassID={0}", classId)
                .AsNoTracking()
                .ToListAsync();

            return resultList;
        }

        public async Task<ResponseDto?> DeleteByIdAsync(int vid)
        {
            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw("EXEC SpDelete_SMSSubject @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();

            return resultList.FirstOrDefault();
        }

        public async Task<ResponseDto> SaveAsync(SMSSubjectSaveDto dto, int userId, string userIp)
        {
            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpSave_SMSSubject @VID={0}, @VName={1}, @ClassID={2}, @IsActive={3}, @UserID={4}, @UserIP={5}",
                    dto.VID,
                    dto.VName,
                    dto.ClassID,
                    dto.IsActive,
                    userId,
                    userIp)
                .AsNoTracking()
                .ToListAsync();

            return resultList.FirstOrDefault();
        }
    }
}

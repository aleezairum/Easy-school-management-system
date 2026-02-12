using Humanizer;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.Academic;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.Models;
using School.API.Repositories.Interfaces.Academic;

namespace School.API.Repositories.Implementations.Academic
{
    public class SMSSectionRepository : ISMSSectionRepository
    {
        private readonly SchoolDbContext _context;

        public SMSSectionRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<List<SMSSectionSaveDto>> GetAllAsync()
        {
            var resultList = await _context
                .Set<SMSSectionSaveDto>()
                .FromSqlRaw("EXEC SpGet_SMSSection @VID={0}", 0) // 0 to get all
                .AsNoTracking()
                .ToListAsync();

            return resultList;
        }


        public async Task<SMSSectionSaveDto?> GetByIdAsync(int vid)
        {
            var resultList = await _context
                .Set<SMSSectionSaveDto>()
                .FromSqlRaw("EXEC SpGet_SMSSection @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();

            return resultList.FirstOrDefault();
        }

        public async Task<ResponseDto?> DeleteByIdAsync(int vid)
        {
            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw("EXEC SpDelete_SMSSection @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();

            return resultList.FirstOrDefault();
        }


        public async Task<ResponseDto> SaveAsync(SMSSectionSaveDto dto, int userId, string userIp)
        {
            // Execute the stored procedure
            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpSave_SMSSection @VID={0}, @VName={1}, @ClassID={2}, @IsActive={3}, @UserID={4}, @UserIP={5}",
                    dto.VID,
                    dto.VName,
                    dto.ClassID,
                    dto.IsActive,
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

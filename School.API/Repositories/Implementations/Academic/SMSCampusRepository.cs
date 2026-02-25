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
    public class SMSCampusRepository : ISMSCampusRepository
    {
        private readonly SchoolDbContext _context;

        public SMSCampusRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<List<SMSCampus>> GetAllAsync()
        {
            var resultList = await _context
                .Set<SMSCampus>()
                .FromSqlRaw("EXEC SpGet_SMSCampus @VID={0}", 0) // 0 to get all
                .AsNoTracking()
                .ToListAsync();

            return resultList;
        }


        public async Task<SMSCampus?> GetByIdAsync(int vid)
        {
            var resultList = await _context
                .Set<SMSCampus>()
                .FromSqlRaw("EXEC SpGet_SMSCampus @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();

            return resultList.FirstOrDefault();
        }

        public async Task<ResponseDto?> DeleteByIdAsync(int vid)
        {
            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw("EXEC SpDelete_SMSCampus @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();

            return resultList.FirstOrDefault();
        }


        public async Task<ResponseDto> SaveAsync(SMSCampusSaveDto dto, int userId, string userIp)
        {
            // Execute the stored procedure
            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpSave_SMSCampus @VID={0}, @VName={1}, @IsActive={2}, @UserID={3}, @UserIP={4}",
                    dto.VID,
                    dto.VName,
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

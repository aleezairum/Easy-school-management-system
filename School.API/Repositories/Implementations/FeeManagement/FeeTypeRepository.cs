using Humanizer;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.Accounts;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.DTOs.FeeManagement;
using School.API.Models;
using School.API.Repositories.Interfaces.Accounts;

namespace School.API.Repositories.Implementations.Accounts
{
    public class FeeTypeRepository : IFeeTypeRepository
    {
        private readonly SchoolDbContext _context;

        public FeeTypeRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<List<SMSFeeType>> GetAllAsync()
        {
            var resultList = await _context
                .Set<SMSFeeType>()
                .FromSqlRaw("EXEC SpGet_SMSFeeType @VID={0}", 0) // 0 to get all
                .AsNoTracking()
                .ToListAsync();

            return resultList;
        }


        public async Task<SMSFeeType?> GetByIdAsync(int vid)
        {
            var resultList = await _context
                .Set<SMSFeeType>()
                .FromSqlRaw("EXEC SpGet_SMSFeeType @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();

            return resultList.FirstOrDefault();
        }

        public async Task<ResponseDto?> DeleteByIdAsync(int vid)
        {
            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw("EXEC SpDelete_SMSFeeType @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();

            return resultList.FirstOrDefault();
        }


        public async Task<ResponseDto> SaveAsync(FeeTypeSaveDto dto, int userId, string userIp)
        {
            // Execute the stored procedure
            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpSave_FeeType @VID={0}, @VName={1}, @Frequency={2}, @IsActive={3}, @UserID={4}, @UserIP={5}",
                    dto.VID,
                    dto.VName,
                    dto.Frequency,
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

using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.Accounts;
using School.API.DTOs.Accounts;
using School.API.DTOs.Common;
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

        public async Task<List<FeeType>> GetAllAsync()
        {
            var resultList = await _context
                .Set<FeeType>()
                .FromSqlRaw("EXEC SpGet_FeeType @VID={0}", 0)
                .AsNoTracking()
                .ToListAsync();

            return resultList;
        }

        public async Task<FeeType?> GetByIdAsync(int vid)
        {
            var resultList = await _context
                .Set<FeeType>()
                .FromSqlRaw("EXEC SpGet_FeeType @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();

            return resultList.FirstOrDefault();
        }

        public async Task<ResponseDto?> DeleteByIdAsync(int vid)
        {
            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw("EXEC SpDelete_FeeType @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();

            return resultList.FirstOrDefault();
        }

        public async Task<ResponseDto> SaveAsync(FeeTypeSaveDto dto, int userId, string userIp)
        {
            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpSave_FeeType @VID={0}, @VName={1}, @IsActive={2}, @UserID={3}, @UserIP={4}",
                    dto.VID,
                    dto.VName,
                    dto.IsActive,
                    userId,
                    userIp)
                .AsNoTracking()
                .ToListAsync();

            var result = resultList.FirstOrDefault();
            return result;
        }
    }
}

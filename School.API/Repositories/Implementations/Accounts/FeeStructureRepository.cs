using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.DTOs.Accounts;
using School.API.DTOs.Common;
using School.API.Repositories.Interfaces.Accounts;

namespace School.API.Repositories.Implementations.Accounts
{
    public class FeeStructureRepository : IFeeStructureRepository
    {
        private readonly SchoolDbContext _context;

        public FeeStructureRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<List<FeeStructureSaveDto>> GetAllAsync()
        {
            var resultList = await _context
                .Set<FeeStructureSaveDto>()
                .FromSqlRaw("EXEC SpGet_FeeStructure @VID={0}", 0)
                .AsNoTracking()
                .ToListAsync();

            return resultList;
        }

        public async Task<FeeStructureSaveDto?> GetByIdAsync(int vid)
        {
            var resultList = await _context
                .Set<FeeStructureSaveDto>()
                .FromSqlRaw("EXEC SpGet_FeeStructure @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();

            return resultList.FirstOrDefault();
        }

        public async Task<ResponseDto?> DeleteByIdAsync(int vid)
        {
            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw("EXEC SpDelete_FeeStructure @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();

            return resultList.FirstOrDefault();
        }

        public async Task<ResponseDto> SaveAsync(FeeStructureSaveDto dto, int userId, string userIp)
        {
            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpSave_FeeStructure @VID={0}, @AcademicSessionID={1}, @ClassID={2}, @GradeID={3}, @FeeTypeID={4}, @Amount={5}, @IsActive={6}, @UserID={7}, @UserIP={8}",
                    dto.VID,
                    dto.AcademicSessionID,
                    dto.ClassID,
                    dto.GradeID,
                    dto.FeeTypeID,
                    dto.Amount,
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

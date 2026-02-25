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
                    "EXEC SpSave_FeeStructure @VID={0}, @CampusID={1}, @AcademicSessionID={2}, @ClassID={3}, @GradeID={4}, @FeeTypeID={5}, @Amount={6}, @IsActive={7}, @UserID={8}, @UserIP={9}",
                    dto.VID,
                    dto.CampusID,
                    dto.AcademicSessionID,
                    dto.ClassID,
                    dto.GradeID ?? 0,
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

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
    public class AcademicSessionYearRepository : IAcademicSessionYearRepository
    {
        private readonly SchoolDbContext _context;

        public AcademicSessionYearRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<List<AcademicSessionYearSaveDto>> GetAllAsync()
        {
            var resultList = await _context
                .Set<AcademicSessionYearSaveDto>()
                .FromSqlRaw("EXEC SpGet_AcademicSessionYear @VID={0}", 0) // 0 to get all
                .AsNoTracking()
                .ToListAsync();

            return resultList;
        }


        public async Task<AcademicSessionYearSaveDto?> GetByIdAsync(int vid)
        {
            var resultList = await _context
                .Set<AcademicSessionYearSaveDto>()
                .FromSqlRaw("EXEC SpGet_AcademicSessionYear @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();

            return resultList.FirstOrDefault();
        }

        public async Task<ResponseDto?> DeleteByIdAsync(int vid)
        {
            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw("EXEC SpDelete_AcademicSessionYear @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();

            return resultList.FirstOrDefault();
        }


        public async Task<ResponseDto> SaveAsync(AcademicSessionYearSaveDto dto, int userId, string userIp)
        {
            // Execute the stored procedure
            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpSave_AcademicSessionYear @VID={0}, @VName={1}, @DateFrom={2}, @DateTo={3}, @IsCurrent={4}, @IsActive={5}, @UserID={6}, @UserIP={7}",
                    dto.VID,
                    dto.VName,
                    dto.DateFrom,
                    dto.DateTo,
                    dto.IsCurrent,
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

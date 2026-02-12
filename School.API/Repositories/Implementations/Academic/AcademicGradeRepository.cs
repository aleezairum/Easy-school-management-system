using Humanizer;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.Academic;
using School.API.DTOs;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.Repositories.Interfaces.Academic;

namespace School.API.Repositories.Implementations.Academic
{
    public class AcademicGradeRepository : IAcademicGradeRepository
    {
        private readonly SchoolDbContext _context;

        public AcademicGradeRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<List<AcademicGrades>> GetAllAsync()
        {
            var resultList = await _context
                .Set<AcademicGrades>()
                .FromSqlRaw("EXEC SpGet_AcademicGrades @VID={0}", 0) // 0 to get all
                .AsNoTracking()
                .ToListAsync();

            return resultList;
        }


        public async Task<AcademicGrades?> GetByIdAsync(int vid)
        {
            var resultList = await _context
                .Set<AcademicGrades>()
                .FromSqlRaw("EXEC SpGet_AcademicGrades @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();

            return resultList.FirstOrDefault();
        }

        public async Task<ResponseDto?> DeleteByIdAsync(int vid)
        {
            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw("EXEC SpDelete_AcademicGrades @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();

            return resultList.FirstOrDefault();
        }


        public async Task<ResponseDto> SaveAsync(AcademicGradeSaveDto dto, int userId, string userIp)
        {
            // Execute the stored procedure
            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpSave_AcademicGrades @VID={0}, @VName={1}, @MinPercentage={2}, @MaxPercentage={3}, @GradePoint={4}, @Remarks={5}, @IsActive={6}, @UserID={7}, @UserIP={8}",
                    dto.VID,
                    dto.VName,
                    dto.MinPercentage,
                    dto.MaxPercentage,
                    dto.GradePoint,
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

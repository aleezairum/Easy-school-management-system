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

        public async Task<List<AcademicGradeSaveDto>> GetAllAsync()
        {
            var resultList = await _context
                .Set<AcademicGradeSaveDto>()
                .FromSqlRaw("EXEC SpGet_AcademicGrades @VID={0}", 0) // 0 to get all
                .AsNoTracking()
                .ToListAsync();

            return resultList;
        }


        public async Task<AcademicGradeSaveDto?> GetByIdAsync(int vid)
        {
            var resultList = await _context
                .Set<AcademicGradeSaveDto>()
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
                    "EXEC SpSave_AcademicGrades @GradeID={0}, @GradeName={1}, @MinPercentage={2}, @MaxPercentage={3}, @GradePoint={4}, @Remarks={5}, @IsActive={6}, @UserID={7}, @UserIP={8}",
                    dto.GradeID,
                    dto.GradeName,
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

        Task<List<AcademicGrades>> IAcademicGradeRepository.GetAllAsync()
        {
            throw new NotImplementedException();
        }

        Task<AcademicGrades?> IAcademicGradeRepository.GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }
    }
}

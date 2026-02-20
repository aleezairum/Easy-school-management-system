using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.Academic;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.Repositories.Interfaces.Academic;

namespace School.API.Repositories.Implementations.Academic
{
    public class CampusRepository : ICampusRepository
    {
        private readonly SchoolDbContext _context;

        public CampusRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<List<Campus>> GetAllAsync()
        {
            var resultList = await _context
                .Set<Campus>()
                .FromSqlRaw("EXEC SpGet_Campus @VID={0}", 0)
                .AsNoTracking()
                .ToListAsync();

            return resultList;
        }

        public async Task<Campus?> GetByIdAsync(int vid)
        {
            var resultList = await _context
                .Set<Campus>()
                .FromSqlRaw("EXEC SpGet_Campus @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();

            return resultList.FirstOrDefault();
        }

        public async Task<ResponseDto?> DeleteByIdAsync(int vid)
        {
            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw("EXEC SpDelete_Campus @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();

            return resultList.FirstOrDefault();
        }

        public async Task<ResponseDto> SaveAsync(CampusSaveDto dto, int userId, string userIp)
        {
            var resultList = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpSave_Campus @VID={0}, @VName={1}, @IsActive={2}, @UserID={3}, @UserIP={4}",
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

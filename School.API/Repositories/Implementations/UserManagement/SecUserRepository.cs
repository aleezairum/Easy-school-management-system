using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.UserManagement;
using School.API.DTOs.Common;
using School.API.DTOs.UserManagement;
using School.API.Repositories.Interfaces.UserManagement;

namespace School.API.Repositories.Implementations.UserManagement
{
    public class SecUserRepository : ISecUserRepository
    {
        private readonly SchoolDbContext _context;

        public SecUserRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<List<SecUser>> GetAllAsync()
        {
            return await _context
                .Set<SecUser>()
                .FromSqlRaw("EXEC SpGet_SecUser @VID={0}", DBNull.Value)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<SecUser?> GetByIdAsync(int vid)
        {
            var result = await _context
                .Set<SecUser>()
                .FromSqlRaw("EXEC SpGet_SecUser @VID={0}", vid)
                .AsNoTracking()
                .ToListAsync();

            return result.FirstOrDefault();
        }

        public async Task<ResponseDto?> DeleteByIdAsync(int vid, int userId, string userIp)
        {
            var result = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpDelete_SecUser @VID={0}, @UserID={1}, @UserIP={2}",
                    vid, userId, userIp)
                .AsNoTracking()
                .ToListAsync();

            return result.FirstOrDefault();
        }

        public async Task<ResponseDto> SaveAsync(UserSaveDto dto, int userId, string userIp)
        {
            var result = await _context
                .Set<ResponseDto>()
                .FromSqlRaw(
                    "EXEC SpSave_SecUser @VID={0}, @EmployeeID={1}, @UserFullName={2}, @UserLogin={3}, @UserPassword={4}, " +
                    "@AllowAudit={5}, @AllowActual={6}, @IsAdmin={7}, @UserType={8}, @IsIndividual={9}, " +
                    "@DefaultCompanyID={10}, @DefaultFinancialYearID={11}, @UserSecFlage={12}, " +
                    "@MacAddress={13}, @ComputerName={14}, @WindowLoginName={15}, @IsActive={16}, " +
                    "@UserID={17}, @UserIP={18}",
                    dto.VID,
                    (object?)dto.EmployeeID ?? DBNull.Value,
                    dto.UserFullName,
                    dto.UserLogin,
                    dto.UserPassword ?? (object)DBNull.Value,
                    dto.AllowAudit,
                    dto.AllowActual,
                    dto.IsAdmin,
                    dto.UserType,
                    dto.IsIndividual,
                    dto.DefaultCompanyID,
                    dto.DefaultFinancialYearID,
                    dto.UserSecFlage,
                    dto.MacAddress ?? (object)DBNull.Value,
                    dto.ComputerName ?? (object)DBNull.Value,
                    dto.WindowLoginName ?? (object)DBNull.Value,
                    dto.IsActive,
                    userId,
                    userIp)
                .AsNoTracking()
                .ToListAsync();

            return result.FirstOrDefault()!;
        }

        public async Task<UserLoginResponseDto?> LoginAsync(UserLoginDto dto)
        {
            var result = await _context
                .Set<UserLoginResponseDto>()
                .FromSqlRaw(
                    "EXEC SpLogin_SecUser @UserLogin={0}, @UserPassword={1}",
                    dto.UserLogin,
                    dto.UserPassword)
                .AsNoTracking()
                .ToListAsync();

            return result.FirstOrDefault();
        }
    }
}

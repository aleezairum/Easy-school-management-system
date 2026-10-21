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

            var response = result.FirstOrDefault()!;

            // Roles are not part of the stored procedure, so they are written
            // here — but only once the user itself saved. ReturnCode 400 is a
            // duplicate login, and assigning roles to a user that was not
            // created would leave rows pointing at nothing.
            if (dto.RoleIds != null && response != null && response.ReturnCode == 200)
            {
                var vid = (int)response.VID;
                if (vid > 0) await ReplaceRolesAsync(vid, dto.RoleIds);
            }

            return response!;
        }

        /// <summary>
        /// Sets a user's roles to exactly this list.
        /// </summary>
        /// <remarks>
        /// UserRoles.UserId holds a SecUser.VID. SecUser is the table the
        /// application actually logs in against, so it is the one the Users
        /// form edits and the one a role has to attach to.
        /// </remarks>
        public async Task ReplaceRolesAsync(int userVid, List<int> roleIds)
        {
            var existing = await _context.SecUserRoles
                .Where(ur => ur.UserVID == userVid)
                .ToListAsync();

            var wanted = roleIds.Where(id => id > 0).Distinct().ToList();

            var remove = existing.Where(ur => !wanted.Contains(ur.RoleId)).ToList();
            if (remove.Count > 0) _context.SecUserRoles.RemoveRange(remove);

            foreach (var roleId in wanted.Where(id => !existing.Any(ur => ur.RoleId == id)))
                _context.SecUserRoles.Add(new SecUserRole { UserVID = userVid, RoleId = roleId, CreatedAt = DateTime.Now });

            await _context.SaveChangesAsync();
        }

        public async Task<List<UserRolesDto>> GetRolesAsync(int? userVid)
        {
            var query = from ur in _context.SecUserRoles.AsNoTracking()
                        join r in _context.Roles.AsNoTracking() on ur.RoleId equals r.Id
                        where userVid == null || ur.UserVID == userVid
                        select new { UserId = ur.UserVID, ur.RoleId, r.RoleName };

            var rows = await query.ToListAsync();

            return rows
                .GroupBy(x => x.UserId)
                .Select(g => new UserRolesDto
                {
                    UserId    = g.Key,
                    RoleIds   = g.Select(x => x.RoleId).ToList(),
                    RoleNames = string.Join(", ", g.Select(x => x.RoleName).OrderBy(n => n))
                })
                .ToList();
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

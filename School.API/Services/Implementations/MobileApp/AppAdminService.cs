using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.MobileApp;
using School.API.Repositories.Implementations.MobileApp;

namespace School.API.Services.Implementations.MobileApp
{
    // ── what the office screens send and get ──
    public class AppLoginRowDto
    {
        public int? AccountId { get; set; }
        public int PersonId { get; set; }
        public string? Code { get; set; }               // roll no / employee code
        public string Name { get; set; } = string.Empty;
        public string? Detail { get; set; }             // father / designation
        public string? ClassName { get; set; }
        public string? SectionName { get; set; }
        public string? LoginId { get; set; }
        public string? InitialPassword { get; set; }    // until the user sets their own
        public bool MustChangePassword { get; set; }
        public bool IsActive { get; set; }
        public int? RoleId { get; set; }
        public string? RoleName { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    public class AppGenerateLoginsDto
    {
        public List<int> StudentIds { get; set; } = new();
    }

    public class AppStaffLoginDto
    {
        public int EmployeeId { get; set; }
        public int RoleId { get; set; }
    }

    // App Usage report: one row per class (students) or designation (staff).
    public class AppUsageRowDto
    {
        public int SortKey { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Total { get; set; }      // logins made
        public int UseApp { get; set; }     // signed in and set their own password
        public int NotUseApp { get; set; }  // still on the PIN from the slip
    }

    public class AppUsageDto
    {
        public List<AppUsageRowDto> Students { get; set; } = new();
        public List<AppUsageRowDto> Staff { get; set; } = new();
    }

    public class AppRoleRowDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public List<string> Features { get; set; } = new();
        public int Logins { get; set; }
    }

    /// <summary>
    /// The office side of the app: making logins, resetting PINs, switching a
    /// login off, and which role may use which app feature.
    /// </summary>
    public class AppAdminService
    {
        private readonly SchoolDbContext _db;
        private readonly AppRepository _repo;
        public AppAdminService(SchoolDbContext db, AppRepository repo) { _db = db; _repo = repo; }

        // ── students ──────────────────────────────────────────────────

        public async Task<List<AppLoginRowDto>> StudentRowsAsync(int sectionId)
        {
            var students = await _repo.GetSectionStudentsAsync(sectionId);
            var ids = students.Select(s => (int?)s.StudentId).ToList();
            var accs = await _db.AppAccounts.AsNoTracking().Where(a => a.AccountType == AppAccountType.Student && ids.Contains(a.StudentId)).ToListAsync();
            return students.Select(s =>
            {
                var a = accs.FirstOrDefault(x => x.StudentId == s.StudentId);
                return new AppLoginRowDto
                {
                    AccountId = a?.Id, PersonId = s.StudentId, Code = s.RollNo, Name = s.Name, Detail = s.FatherName,
                    ClassName = s.ClassName, SectionName = s.SectionName, LoginId = a?.LoginId, InitialPassword = a?.InitialPassword,
                    MustChangePassword = a?.MustChangePassword ?? false, IsActive = a?.IsActive ?? false, LastLoginAt = a?.LastLoginAt
                };
            }).ToList();
        }

        /// <summary>
        /// Makes a login for each student who has none: the login ID is the roll
        /// number (S + id when the roll number is blank or already taken), the
        /// password a new 6-digit PIN printed on the login slip.
        /// </summary>
        public async Task<int> GenerateStudentLoginsAsync(List<int> studentIds, int? userId)
        {
            var made = 0;
            foreach (var id in studentIds.Distinct())
            {
                if (await _db.AppAccounts.AnyAsync(a => a.StudentId == id)) continue;
                var s = await _repo.GetStudentAsync(id);
                if (s is not { Enrolled: true }) continue;
                var login = string.IsNullOrWhiteSpace(s.Value.Card.RollNo) ? $"S{id}" : s.Value.Card.RollNo!.Trim();
                if (await _db.AppAccounts.AnyAsync(a => a.AccountType == AppAccountType.Student && a.LoginId == login)) login = $"S{id}";
                var pin = AppPassword.NewPin();
                var (hash, salt) = AppPassword.Hash(pin);
                _db.AppAccounts.Add(new AppAccount
                {
                    AccountType = AppAccountType.Student, StudentId = id, LoginId = login, DisplayName = s.Value.Card.Name,
                    PasswordHash = hash, PasswordSalt = salt, InitialPassword = pin, MustChangePassword = true, CreatedBy = userId
                });
                await _db.SaveChangesAsync();
                made++;
            }
            return made;
        }

        // ── staff ─────────────────────────────────────────────────────

        public async Task<List<AppLoginRowDto>> StaffRowsAsync(int? campusId)
        {
            var staff = await _db.Database.SqlQueryRaw<StaffRow>(@"
                SELECT E.Id, E.EmployeeCode, LTRIM(RTRIM(E.FirstName + ' ' + ISNULL(E.LastName,''))) AS Name, D.Name AS Designation
                FROM Employees E LEFT JOIN Designations D ON D.Id = E.DesignationId
                WHERE E.IsActive = 1 AND ({0} IS NULL OR E.CampusID = {0})", (object?)campusId ?? DBNull.Value).ToListAsync();
            var accs = await _db.AppAccounts.AsNoTracking().Where(a => a.AccountType == AppAccountType.Staff).ToListAsync();
            var roles = await _db.Roles.AsNoTracking().ToDictionaryAsync(r => r.Id, r => r.RoleName);
            return staff.OrderBy(s => s.EmployeeCode).Select(s =>
            {
                var a = accs.FirstOrDefault(x => x.EmployeeId == s.Id);
                return new AppLoginRowDto
                {
                    AccountId = a?.Id, PersonId = s.Id, Code = s.EmployeeCode, Name = s.Name, Detail = s.Designation,
                    LoginId = a?.LoginId, InitialPassword = a?.InitialPassword, MustChangePassword = a?.MustChangePassword ?? false,
                    IsActive = a?.IsActive ?? false, RoleId = a?.RoleId, RoleName = a?.RoleId is int r && roles.TryGetValue(r, out var n) ? n : null,
                    LastLoginAt = a?.LastLoginAt
                };
            }).ToList();
        }

        public sealed class StaffRow { public int Id { get; set; } public string? EmployeeCode { get; set; } public string Name { get; set; } = ""; public string? Designation { get; set; } }

        /// <summary>Makes the staff login (ID = employee code, new PIN), or changes the role of an existing one.</summary>
        public async Task<AppLoginRowDto> SaveStaffLoginAsync(AppStaffLoginDto dto, int? userId)
        {
            if (!await _db.Roles.AnyAsync(r => r.Id == dto.RoleId)) throw new InvalidOperationException("Choose a role.");
            var acc = await _db.AppAccounts.FirstOrDefaultAsync(a => a.EmployeeId == dto.EmployeeId);
            if (acc == null)
            {
                var emp = (await _repo.GetStaffAsync(dto.EmployeeId)) ?? throw new KeyNotFoundException();
                var login = string.IsNullOrWhiteSpace(emp.Card.EmployeeCode) ? $"E{dto.EmployeeId}" : emp.Card.EmployeeCode!.Trim();
                if (await _db.AppAccounts.AnyAsync(a => a.AccountType == AppAccountType.Staff && a.LoginId == login)) login = $"E{dto.EmployeeId}";
                var pin = AppPassword.NewPin();
                var (hash, salt) = AppPassword.Hash(pin);
                _db.AppAccounts.Add(acc = new AppAccount
                {
                    AccountType = AppAccountType.Staff, EmployeeId = dto.EmployeeId, LoginId = login, DisplayName = emp.Card.Name,
                    PasswordHash = hash, PasswordSalt = salt, InitialPassword = pin, MustChangePassword = true, CreatedBy = userId
                });
            }
            acc.RoleId = dto.RoleId;
            acc.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return (await StaffRowsAsync(null)).First(r => r.PersonId == dto.EmployeeId);
        }

        // ── any login ─────────────────────────────────────────────────

        /// <summary>A new PIN (the user must change it again at the next sign-in); returns it for the slip.</summary>
        public async Task<string> ResetAsync(int accountId)
        {
            var acc = await _db.AppAccounts.FirstOrDefaultAsync(a => a.Id == accountId) ?? throw new KeyNotFoundException();
            var pin = AppPassword.NewPin();
            (acc.PasswordHash, acc.PasswordSalt) = AppPassword.Hash(pin);
            acc.InitialPassword = pin; acc.MustChangePassword = true; acc.FailedLogins = 0; acc.LockedUntil = null; acc.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return pin;
        }

        /// <summary>Switches a login on or off; off also signs out its phones (no more notifications).</summary>
        public async Task SetActiveAsync(int accountId, bool active)
        {
            var acc = await _db.AppAccounts.FirstOrDefaultAsync(a => a.Id == accountId) ?? throw new KeyNotFoundException();
            acc.IsActive = active; acc.UpdatedAt = DateTime.UtcNow;
            if (!active) _db.AppDevices.RemoveRange(_db.AppDevices.Where(d => d.AccountId == accountId));
            await _db.SaveChangesAsync();
        }

        // ── usage report ──────────────────────────────────────────────

        /// <summary>
        /// How many of the logins made are in use, class by class and, for staff,
        /// designation by designation. A login is "in use" once its owner has signed
        /// in and set their own password (the first sign-in insists on it); one still
        /// on the slip's PIN has never been used. A student counts in their current
        /// class only (latest enrolment), and only while enrolled.
        /// </summary>
        public async Task<AppUsageDto> UsageAsync(int? campusId)
        {
            var campus = (object?)campusId ?? DBNull.Value;
            var students = await _db.Database.SqlQueryRaw<AppUsageRowDto>(@"
                SELECT C.VID AS SortKey, C.VName AS Name, COUNT(*) AS Total,
                       SUM(CASE WHEN H.MustChangePassword = 0 THEN 1 ELSE 0 END) AS UseApp,
                       SUM(CASE WHEN H.MustChangePassword = 1 THEN 1 ELSE 0 END) AS NotUseApp
                FROM AppAccount H
                JOIN SMSStudent ST ON ST.VID = H.StudentId
                CROSS APPLY (SELECT TOP 1 X.SectionID, X.StatusID FROM SMSStudentAcademic X
                             WHERE X.StudentID = H.StudentId AND ISNULL(X.IsActive, 1) = 1
                             ORDER BY X.SessionID DESC, X.VID DESC) A
                JOIN SMSSection S ON S.VID = A.SectionID
                JOIN SMSClass   C ON C.VID = S.ClassID
                WHERE H.AccountType = 1 AND A.StatusID = 1
                  AND ({0} IS NULL OR ST.CampusID = {0})
                GROUP BY C.VID, C.VName", campus).ToListAsync();
            var staff = await _db.Database.SqlQueryRaw<AppUsageRowDto>(@"
                SELECT 0 AS SortKey, ISNULL(D.Name, 'Other') AS Name, COUNT(*) AS Total,
                       SUM(CASE WHEN H.MustChangePassword = 0 THEN 1 ELSE 0 END) AS UseApp,
                       SUM(CASE WHEN H.MustChangePassword = 1 THEN 1 ELSE 0 END) AS NotUseApp
                FROM AppAccount H
                JOIN Employees E ON E.Id = H.EmployeeId
                LEFT JOIN Designations D ON D.Id = E.DesignationId
                WHERE H.AccountType = 2 AND E.IsActive = 1
                  AND ({0} IS NULL OR E.CampusID = {0})
                GROUP BY D.Name", campus).ToListAsync();
            return new AppUsageDto
            {
                Students = students.OrderBy(r => r.SortKey).ToList(),
                Staff = staff.OrderByDescending(r => r.Total).ThenBy(r => r.Name).ToList()
            };
        }

        // ── roles and features ────────────────────────────────────────

        public async Task<List<AppRoleRowDto>> RolesAsync()
        {
            var roles = await _db.Roles.AsNoTracking().Where(r => r.IsActive).OrderBy(r => r.RoleName).ToListAsync();
            var features = await _db.AppRoleFeatures.AsNoTracking().ToListAsync();
            var counts = await _db.AppAccounts.Where(a => a.RoleId != null).GroupBy(a => a.RoleId).Select(g => new { g.Key, N = g.Count() }).ToListAsync();
            return roles.Select(r => new AppRoleRowDto
            {
                RoleId = r.Id, RoleName = r.RoleName, IsAdmin = string.Equals(r.RoleName, "Admin", StringComparison.OrdinalIgnoreCase),
                Features = features.Where(f => f.RoleId == r.Id).Select(f => f.FeatureKey).ToList(),
                Logins = counts.FirstOrDefault(c => c.Key == r.Id)?.N ?? 0
            }).ToList();
        }

        public async Task SetRoleFeaturesAsync(int roleId, List<string> keys)
        {
            if (!await _db.Roles.AnyAsync(r => r.Id == roleId)) throw new KeyNotFoundException();
            var valid = AppFeatures.All.Select(f => f.Key).ToHashSet();
            _db.AppRoleFeatures.RemoveRange(_db.AppRoleFeatures.Where(f => f.RoleId == roleId));
            _db.AppRoleFeatures.AddRange(keys.Where(valid.Contains).Distinct().Select(k => new AppRoleFeature { RoleId = roleId, FeatureKey = k }));
            await _db.SaveChangesAsync();
        }
    }
}

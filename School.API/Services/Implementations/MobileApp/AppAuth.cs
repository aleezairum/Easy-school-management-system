using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using School.API.Data;
using School.API.Data.DBModels.MobileApp;

namespace School.API.Services.Implementations.MobileApp
{
    /// <summary>Password hashing for app logins: PBKDF2-SHA256, a salt per account.</summary>
    public static class AppPassword
    {
        private const int Iterations = 100_000;

        public static (byte[] Hash, byte[] Salt) Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            return (Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32), salt);
        }

        public static bool Verify(string password, byte[] hash, byte[] salt) =>
            CryptographicOperations.FixedTimeEquals(
                Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32), hash);

        /// <summary>A 6-digit PIN for a new login slip - easy to type on a phone, changed at first sign-in.</summary>
        public static string NewPin() => RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    }

    /// <summary>
    /// Who is calling the app API, from the token - and, because a login can be
    /// switched off at any time, checked against the account on every request.
    /// </summary>
    public sealed class AppCaller
    {
        public int AccountId { get; init; }
        public string Mode { get; init; } = "student";       // student | parent | staff
        public int? StudentId { get; init; }
        public int? ParentId { get; init; }
        public int? EmployeeId { get; init; }
        public int? RoleId { get; init; }
        public string? RoleName { get; init; }
        public bool IsAdmin { get; init; }
        public HashSet<string> Features { get; init; } = new();
        public string Name { get; init; } = string.Empty;
        public string LoginId { get; init; } = string.Empty;

        public bool IsStaff => Mode == "staff";
        public bool Can(string feature) => IsAdmin || Features.Contains(feature);
    }

    /// <summary>The claims an app token carries; nothing else in the API issues these.</summary>
    public static class AppClaims
    {
        public const string Account = "app_acc";
        public const string Mode = "app_mode";
        public const string Policy = "App";
    }

    public class AppTokenIssuer
    {
        private readonly IConfiguration _config;
        public AppTokenIssuer(IConfiguration config) => _config = config;

        public (string Token, DateTime ExpiresAt) Issue(AppAccount account, string mode)
        {
            var days = _config.GetValue("App:TokenDays", 60);
            var expires = DateTime.UtcNow.AddDays(days);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var token = new JwtSecurityToken(
                claims: new[]
                {
                    new Claim(AppClaims.Account, account.Id.ToString()),
                    new Claim(AppClaims.Mode, mode),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                },
                expires: expires,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
            return (new JwtSecurityTokenHandler().WriteToken(token), expires);
        }
    }

    /// <summary>Turns the token into an <see cref="AppCaller"/>, or null when the login is no longer valid.</summary>
    public class AppCallerResolver
    {
        private readonly SchoolDbContext _db;
        public AppCallerResolver(SchoolDbContext db) => _db = db;

        public async Task<AppCaller?> ResolveAsync(ClaimsPrincipal user)
        {
            if (!int.TryParse(user.FindFirst(AppClaims.Account)?.Value, out var id)) return null;
            var mode = user.FindFirst(AppClaims.Mode)?.Value ?? "";
            var acc = await _db.AppAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            if (acc == null || !acc.IsActive) return null;
            if (acc.AccountType == AppAccountType.Student && mode is not ("student" or "parent")) return null;
            if (acc.AccountType == AppAccountType.Staff && mode != "staff") return null;

            int? parentId = null;
            if (acc.StudentId.HasValue)
                parentId = await _db.Database.SqlQueryRaw<int?>(
                    "SELECT ParentID AS Value FROM SMSStudent WHERE VID = {0}", acc.StudentId.Value).FirstOrDefaultAsync();

            string? roleName = null; var features = new HashSet<string>();
            if (acc.RoleId.HasValue)
            {
                roleName = await _db.Roles.Where(r => r.Id == acc.RoleId).Select(r => r.RoleName).FirstOrDefaultAsync();
                features = (await _db.AppRoleFeatures.Where(f => f.RoleId == acc.RoleId).Select(f => f.FeatureKey).ToListAsync()).ToHashSet();
            }

            return new AppCaller
            {
                AccountId = acc.Id,
                Mode = mode,
                StudentId = acc.StudentId,
                ParentId = mode == "parent" ? parentId : null,
                EmployeeId = acc.EmployeeId,
                RoleId = acc.RoleId,
                RoleName = roleName,
                IsAdmin = acc.AccountType == AppAccountType.Staff && string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase),
                Features = features,
                Name = acc.DisplayName ?? acc.LoginId,
                LoginId = acc.LoginId
            };
        }
    }
}

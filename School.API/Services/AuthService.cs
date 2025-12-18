using Microsoft.IdentityModel.Tokens;
using School.API.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace School.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;

        public AuthService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public AuthResponseDto? Login(LoginDto dto)
        {
            // TEMPORARY MOCK USER (replace when DB is ready)
            if (dto.Email != "admin@school.com" || dto.Password != "admin123")
                return null;

            var jwtKey = _configuration["Jwt:Key"] ?? "your_super_secret_key_123_must_be_at_least_32_characters";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiresAt = DateTime.UtcNow.AddHours(12);

            var claims = new[]
            {
                new Claim(ClaimTypes.Email, dto.Email),
                new Claim(ClaimTypes.Name, "Admin User"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: expiresAt,
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return new AuthResponseDto
            {
                Token = tokenString,
                Email = dto.Email,
                FullName = "Admin User",
                Role = "Admin",
                ExpiresAt = expiresAt
            };
        }
    }
}

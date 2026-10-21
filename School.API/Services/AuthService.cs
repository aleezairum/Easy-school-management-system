using School.API.DTOs;

namespace School.API.Services
{
    /// <summary>
    /// Retired. This was a hard-coded admin@school.com / admin123 stub that
    /// handed out tokens to anyone who knew it. The web portal signs in through
    /// api/SecUser/login (School.Web then signs the API token), and the mobile
    /// app through api/app/auth/login. Kept only so api/Auth/login answers
    /// "invalid" instead of disappearing.
    /// </summary>
    public class AuthService : IAuthService
    {
        public AuthResponseDto? Login(LoginDto dto) => null;
    }
}

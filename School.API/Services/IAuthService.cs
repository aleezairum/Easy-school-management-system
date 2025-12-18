using School.API.DTOs;

namespace School.API.Services
{
    public interface IAuthService
    {
        AuthResponseDto? Login(LoginDto dto);
    }
}

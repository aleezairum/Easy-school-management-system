using School.API.Data.DBModels.UserManagement;
using School.API.DTOs.Common;
using School.API.DTOs.UserManagement;

namespace School.API.Repositories.Interfaces.UserManagement
{
    public interface ISecUserRepository
    {
        Task<ResponseDto> SaveAsync(UserSaveDto dto, int userId, string userIp);
        Task<List<SecUser>> GetAllAsync();
        Task<SecUser?> GetByIdAsync(int id);
        Task<ResponseDto?> DeleteByIdAsync(int id, int userId, string userIp);
        Task<UserLoginResponseDto?> LoginAsync(UserLoginDto dto);
    }
}

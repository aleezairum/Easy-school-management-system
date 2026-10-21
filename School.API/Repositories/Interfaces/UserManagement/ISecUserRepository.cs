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
        /// <summary>Sets a user's roles to exactly this list.</summary>
        Task ReplaceRolesAsync(int userVid, List<int> roleIds);

        /// <summary>Role assignments — one user when given a VID, everyone when null.</summary>
        Task<List<UserRolesDto>> GetRolesAsync(int? userVid);
        Task<UserLoginResponseDto?> LoginAsync(UserLoginDto dto);
    }
}

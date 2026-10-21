using School.API.Data.DBModels.UserManagement;
using School.API.DTOs.Common;
using School.API.DTOs.UserManagement;
using School.API.Repositories.Interfaces.UserManagement;
using School.API.Services.Interfaces.UserManagement;

namespace School.API.Services.Implementations.UserManagement
{
    public class SecUserService : ISecUserService
    {
        private readonly ISecUserRepository _repo;

        public SecUserService(ISecUserRepository repo)
        {
            _repo = repo;
        }

        public Task<ResponseDto> SaveAsync(UserSaveDto dto, int userId, string userIp)
            => _repo.SaveAsync(dto, userId, userIp);

        public Task<List<SecUser>> GetAllAsync()
            => _repo.GetAllAsync();

        public Task<SecUser?> GetByIdAsync(int id)
            => _repo.GetByIdAsync(id);

        public Task<ResponseDto?> DeleteByIdAsync(int id, int userId, string userIp)
            => _repo.DeleteByIdAsync(id, userId, userIp);

        public Task<List<UserRolesDto>> GetRolesAsync(int? userVid)
            => _repo.GetRolesAsync(userVid);

        public Task ReplaceRolesAsync(int userVid, List<int> roleIds)
            => _repo.ReplaceRolesAsync(userVid, roleIds);

        public Task<UserLoginResponseDto?> LoginAsync(UserLoginDto dto)
            => _repo.LoginAsync(dto);
    }
}

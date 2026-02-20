using School.API.Data.DBModels.Academic;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.Repositories.Interfaces.Academic;
using School.API.Services.Interfaces.Academic;

namespace School.API.Services.Implementations.Academic
{
    public class CampusService : ICampusService
    {
        private readonly ICampusRepository _repo;

        public CampusService(ICampusRepository repo)
        {
            _repo = repo;
        }

        public Task<ResponseDto> SaveAsync(
            CampusSaveDto dto,
            int userId,
            string userIp)
            => _repo.SaveAsync(dto, userId, userIp);

        public Task<List<Campus>> GetAllAsync()
            => _repo.GetAllAsync();

        public Task<Campus?> GetByIdAsync(int id)
            => _repo.GetByIdAsync(id);

        public Task<ResponseDto?> DeleteByIdAsync(int id)
           => _repo.DeleteByIdAsync(id);
    }
}

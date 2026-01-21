using School.API.Data.DBModels.Academic;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.Repositories.Implementations.Academic;
using School.API.Repositories.Interfaces.Academic;
using School.API.Services.Interfaces.Academic;

namespace School.API.Services.Implementations.Academic
{
    public class SMSSectionService : ISMSSectionService
    {
        private readonly ISMSSectionRepository _repo;

        public SMSSectionService(ISMSSectionRepository repo)
        {
            _repo = repo;
        }

        public Task<ResponseDto> SaveAsync(
            SMSSectionSaveDto dto,
            int userId,
            string userIp)
            => _repo.SaveAsync(dto, userId, userIp);

        public Task<List<SMSSectionSaveDto>> GetAllAsync()
            => _repo.GetAllAsync();

        public Task<SMSSectionSaveDto?> GetByIdAsync(int id)
            => _repo.GetByIdAsync(id);

        public Task<ResponseDto?> DeleteByIdAsync(int id)
           => _repo.DeleteByIdAsync(id);
    }
}

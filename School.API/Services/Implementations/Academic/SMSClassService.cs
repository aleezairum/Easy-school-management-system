using School.API.Data.DBModels.Academic;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.Repositories.Implementations.Academic;
using School.API.Repositories.Interfaces.Academic;
using School.API.Services.Interfaces.Academic;

namespace School.API.Services.Implementations.Academic
{
    public class SMSClassService : ISMSClassService
    {
        private readonly ISMSClassRepository _repo;

        public SMSClassService(ISMSClassRepository repo)
        {
            _repo = repo;
        }

        public Task<ResponseDto> SaveAsync(
            SMSClassSaveDto dto,
            int userId,
            string userIp)
            => _repo.SaveAsync(dto, userId, userIp);

        public Task<List<SMSClass>> GetAllAsync()
            => _repo.GetAllAsync();

        public Task<SMSClass?> GetByIdAsync(int id)
            => _repo.GetByIdAsync(id);

        public Task<ResponseDto?> DeleteByIdAsync(int id)
           => _repo.DeleteByIdAsync(id);
    }
}

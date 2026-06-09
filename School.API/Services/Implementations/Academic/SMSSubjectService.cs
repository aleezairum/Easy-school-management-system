using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.Repositories.Interfaces.Academic;
using School.API.Services.Interfaces.Academic;

namespace School.API.Services.Implementations.Academic
{
    public class SMSSubjectService : ISMSSubjectService
    {
        private readonly ISMSSubjectRepository _repo;

        public SMSSubjectService(ISMSSubjectRepository repo)
        {
            _repo = repo;
        }

        public Task<ResponseDto> SaveAsync(
            SMSSubjectSaveDto dto,
            int userId,
            string userIp)
            => _repo.SaveAsync(dto, userId, userIp);

        public Task<List<SMSSubjectSaveDto>> GetAllAsync()
            => _repo.GetAllAsync();

        public Task<SMSSubjectSaveDto?> GetByIdAsync(int id)
            => _repo.GetByIdAsync(id);

        public Task<List<SMSSubjectSaveDto>> FillComboAsync(int id)
            => _repo.FillComboAsync(id);

        public Task<ResponseDto?> DeleteByIdAsync(int id)
            => _repo.DeleteByIdAsync(id);
    }
}

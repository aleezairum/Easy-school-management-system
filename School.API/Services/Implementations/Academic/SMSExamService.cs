using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.Repositories.Interfaces.Academic;
using School.API.Services.Interfaces.Academic;

namespace School.API.Services.Implementations.Academic
{
    public class SMSExamService : ISMSExamService
    {
        private readonly ISMSExamRepository _repo;

        public SMSExamService(ISMSExamRepository repo)
        {
            _repo = repo;
        }

        public Task<ResponseDto> SaveAsync(SMSExamSaveDto dto, int userId, string userIp)
            => _repo.SaveAsync(dto, userId, userIp);

        public Task<List<SMSExamSaveDto>> GetAllAsync()
            => _repo.GetAllAsync();

        public Task<SMSExamSaveDto?> GetByIdAsync(int id)
            => _repo.GetByIdAsync(id);

        public Task<List<SMSExamSaveDto>> FillComboAsync()
            => _repo.FillComboAsync();

        public Task<ResponseDto?> DeleteByIdAsync(int id)
            => _repo.DeleteByIdAsync(id);
    }
}

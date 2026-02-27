using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.DTOs.FeeManagement;
using School.API.Repositories.Interfaces.Academic;
using School.API.Repositories.Interfaces.Accounts;
using School.API.Services.Interfaces.Academic;
using School.API.Services.Interfaces.Accounts;

namespace School.API.Services.Implementations.Accounts
{
    public class FeeStructureService : IFeeStructureService
    {
        private readonly IFeeStructureRepository _repo;

        public FeeStructureService(IFeeStructureRepository repo)
        {
            _repo = repo;
        }

        public Task<ResponseDto> SaveAsync(FeeStructureSaveDto dto, int userId, string userIp)
            => _repo.SaveAsync(dto, userId, userIp);

        public Task<List<FeeStructureSaveDto>> GetAllAsync()
            => _repo.GetAllAsync();

        public Task<FeeStructureSaveDto?> GetByIdAsync(int id)
            => _repo.GetByIdAsync(id);

        public Task<ResponseDto?> DeleteByIdAsync(int id)
            => _repo.DeleteByIdAsync(id);
    }
}

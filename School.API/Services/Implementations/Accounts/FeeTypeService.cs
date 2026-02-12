using School.API.Data.DBModels.Accounts;
using School.API.DTOs.Accounts;
using School.API.DTOs.Common;
using School.API.Repositories.Interfaces.Accounts;
using School.API.Services.Interfaces.Accounts;

namespace School.API.Services.Implementations.Accounts
{
    public class FeeTypeService : IFeeTypeService
    {
        private readonly IFeeTypeRepository _repo;

        public FeeTypeService(IFeeTypeRepository repo)
        {
            _repo = repo;
        }

        public Task<ResponseDto> SaveAsync(FeeTypeSaveDto dto, int userId, string userIp)
            => _repo.SaveAsync(dto, userId, userIp);

        public Task<List<FeeType>> GetAllAsync()
            => _repo.GetAllAsync();

        public Task<FeeType?> GetByIdAsync(int id)
            => _repo.GetByIdAsync(id);

        public Task<ResponseDto?> DeleteByIdAsync(int id)
            => _repo.DeleteByIdAsync(id);
    }
}

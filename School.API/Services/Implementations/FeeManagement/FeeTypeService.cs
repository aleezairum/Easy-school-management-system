using School.API.Data.DBModels.Academic;
using School.API.Data.DBModels.Accounts;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.DTOs.FeeManagement;
using School.API.Repositories.Implementations.Academic;
using School.API.Repositories.Interfaces.Academic;
using School.API.Repositories.Interfaces.Accounts;
using School.API.Services.Interfaces.Academic;
using School.API.Services.Interfaces.Accounts;

namespace School.API.Services.Implementations.Academic
{
    public class FeeTypeService : ISMSFeeTypeService
    {
        private readonly IFeeTypeRepository _repo;

        public FeeTypeService(IFeeTypeRepository repo)
        {
            _repo = repo;
        }

        public Task<ResponseDto> SaveAsync(
            FeeTypeSaveDto dto,
            int userId,
            string userIp)
            => _repo.SaveAsync(dto, userId, userIp);

        public Task<List<SMSFeeType>> GetAllAsync()
            => _repo.GetAllAsync();

        public Task<SMSFeeType?> GetByIdAsync(int id)
            => _repo.GetByIdAsync(id);

        public Task<ResponseDto?> DeleteByIdAsync(int id)
           => _repo.DeleteByIdAsync(id);
    }
}

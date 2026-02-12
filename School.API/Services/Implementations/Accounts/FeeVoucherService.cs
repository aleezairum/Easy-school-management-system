using School.API.DTOs;
using School.API.Repositories.Interfaces.Accounts;
using School.API.Services.Interfaces.Accounts;

namespace School.API.Services.Implementations.Accounts
{
    public class FeeVoucherService : IFeeVoucherService
    {
        private readonly IFeeVoucherRepository _repository;

        public FeeVoucherService(IFeeVoucherRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<FeeVoucherListDto>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<FeeVoucherDto?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<FeeVoucherListDto>> GetByChallanAsync(int challanVoucherId)
        {
            return await _repository.GetByChallanAsync(challanVoucherId);
        }

        public async Task<IEnumerable<FeeVoucherListDto>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            return await _repository.GetByDateRangeAsync(fromDate, toDate);
        }

        public async Task<FeeVoucherDto> CreateAsync(CreateFeeVoucherDto dto)
        {
            var entity = await _repository.CreateAsync(dto);
            var result = await _repository.GetByIdAsync(entity.Id);
            return result!;
        }

        public async Task<FeeVoucherDto?> UpdateAsync(int id, UpdateFeeVoucherDto dto)
        {
            var entity = await _repository.UpdateAsync(id, dto);
            if (entity == null)
                return null;

            return await _repository.GetByIdAsync(id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }
    }
}

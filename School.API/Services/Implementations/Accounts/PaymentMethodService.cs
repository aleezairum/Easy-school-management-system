using School.API.DTOs;
using School.API.Repositories.Interfaces.Accounts;
using School.API.Services.Interfaces.Accounts;

namespace School.API.Services.Implementations.Accounts
{
    public class PaymentMethodService : IPaymentMethodService
    {
        private readonly IPaymentMethodRepository _repository;

        public PaymentMethodService(IPaymentMethodRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<PaymentMethodListDto>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<PaymentMethodDto?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<PaymentMethodDto> CreateAsync(CreatePaymentMethodDto dto)
        {
            var entity = await _repository.CreateAsync(dto);
            return new PaymentMethodDto
            {
                Id = entity.Id,
                Code = entity.Code,
                Name = entity.Name,
                Description = entity.Description,
                BankName = entity.BankName,
                AccountNumber = entity.AccountNumber,
                BranchCode = entity.BranchCode,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        public async Task<PaymentMethodDto?> UpdateAsync(int id, UpdatePaymentMethodDto dto)
        {
            var entity = await _repository.UpdateAsync(id, dto);
            if (entity == null)
                return null;

            return new PaymentMethodDto
            {
                Id = entity.Id,
                Code = entity.Code,
                Name = entity.Name,
                Description = entity.Description,
                BankName = entity.BankName,
                AccountNumber = entity.AccountNumber,
                BranchCode = entity.BranchCode,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        public async Task<IEnumerable<PaymentMethodDropdownDto>> GetDropdownAsync()
        {
            return await _repository.GetDropdownAsync();
        }
    }
}

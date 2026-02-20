using School.API.Data.DBModels.Accounts;
using School.API.DTOs;

namespace School.API.Repositories.Interfaces.Accounts
{
    public interface IPaymentMethodRepository
    {
        Task<IEnumerable<PaymentMethodListDto>> GetAllAsync();
        Task<PaymentMethodDto?> GetByIdAsync(int id);
        Task<PaymentMethod> CreateAsync(CreatePaymentMethodDto dto);
        Task<PaymentMethod?> UpdateAsync(int id, UpdatePaymentMethodDto dto);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<PaymentMethodDropdownDto>> GetDropdownAsync();
        Task<bool> ExistsAsync(int id);
        Task<bool> CodeExistsAsync(string code, int? excludeId = null);
    }
}

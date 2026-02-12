using School.API.DTOs;

namespace School.API.Services.Interfaces.Accounts
{
    public interface IPaymentMethodService
    {
        Task<IEnumerable<PaymentMethodListDto>> GetAllAsync();
        Task<PaymentMethodDto?> GetByIdAsync(int id);
        Task<PaymentMethodDto> CreateAsync(CreatePaymentMethodDto dto);
        Task<PaymentMethodDto?> UpdateAsync(int id, UpdatePaymentMethodDto dto);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<PaymentMethodDropdownDto>> GetDropdownAsync();
    }
}

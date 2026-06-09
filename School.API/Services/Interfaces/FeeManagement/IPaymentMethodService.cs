using School.API.Data.DBModels.Academic;
using School.API.Data.DBModels.Accounts;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.DTOs.FeeManagement;
using School.API.Repositories.Interfaces.Academic;
using School.API.Repositories.Interfaces.Accounts;

namespace School.API.Services.Interfaces.Accounts
{
    public interface IPaymentMethodService
    {
        Task<ResponseDto> SaveAsync(
            PaymentMethodDto dto,
            int userId,
            string userIp);

        Task<List<PaymentMethod>> GetAllAsync();
        Task<PaymentMethod?> GetByIdAsync(int id);
        Task<ResponseDto?> DeleteByIdAsync(int id);
    }
}

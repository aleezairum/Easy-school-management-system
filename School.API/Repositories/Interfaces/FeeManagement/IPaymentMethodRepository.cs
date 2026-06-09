using School.API.Data.DBModels.Academic;
using School.API.Data.DBModels.Accounts;
using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.DTOs.FeeManagement;

namespace School.API.Repositories.Interfaces.Accounts
{
    public interface IPaymentMethodRepository
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

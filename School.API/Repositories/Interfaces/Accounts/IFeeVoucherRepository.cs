using School.API.Data.DBModels.Accounts;
using School.API.DTOs;

namespace School.API.Repositories.Interfaces.Accounts
{
    public interface IFeeVoucherRepository
    {
        Task<IEnumerable<FeeVoucherListDto>> GetAllAsync();
        Task<FeeVoucherDto?> GetByIdAsync(int id);
        Task<IEnumerable<FeeVoucherListDto>> GetByChallanAsync(int challanVoucherId);
        Task<IEnumerable<FeeVoucherListDto>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate);
        Task<FeeVoucher> CreateAsync(CreateFeeVoucherDto dto);
        Task<FeeVoucher?> UpdateAsync(int id, UpdateFeeVoucherDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<string> GenerateVoucherNumberAsync();
    }
}

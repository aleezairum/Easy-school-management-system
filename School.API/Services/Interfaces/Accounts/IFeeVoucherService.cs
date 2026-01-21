using School.API.DTOs;

namespace School.API.Services.Interfaces.Accounts
{
    public interface IFeeVoucherService
    {
        Task<IEnumerable<FeeVoucherListDto>> GetAllAsync();
        Task<FeeVoucherDto?> GetByIdAsync(int id);
        Task<IEnumerable<FeeVoucherListDto>> GetByChallanAsync(int challanVoucherId);
        Task<IEnumerable<FeeVoucherListDto>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate);
        Task<FeeVoucherDto> CreateAsync(CreateFeeVoucherDto dto);
        Task<FeeVoucherDto?> UpdateAsync(int id, UpdateFeeVoucherDto dto);
        Task<bool> DeleteAsync(int id);
    }
}

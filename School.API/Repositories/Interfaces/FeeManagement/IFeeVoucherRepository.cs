using School.API.Data.DBModels.Accounts;
using School.API.DTOs.Common;
using School.API.DTOs.FeeManagement;

namespace School.API.Repositories.Interfaces.Accounts
{
    public interface IFeeVoucherRepository
    {
        Task<FeeVoucherMain?> GetByIdAsync(int id);
        Task<ResponseDto> SaveAsync(FeeVoucherSaveDto dto, int userId, string userIp);
        Task<string> GetNextVNoAsync();
        Task<FeeVoucherReceiptDto?> GetReceiptAsync(decimal id);
        Task<List<FeeVoucherListDto>> GetAllAsync(int? classId, int? sectionId, int? studentId, DateTime dateFrom, DateTime dateTo);
        Task<ResponseDto> DeleteByIdAsync(int id, int userId, string userIp);

    }
}
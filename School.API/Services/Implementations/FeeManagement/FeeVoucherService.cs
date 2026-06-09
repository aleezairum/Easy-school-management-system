using School.API.Data.DBModels.Accounts;
using School.API.DTOs.Common;
using School.API.DTOs.FeeManagement;
using School.API.Repositories.Interfaces.Accounts;
using School.API.Services.Interfaces.Accounts;

namespace School.API.Services.Implementations.Accounts
{
    public class FeeVoucherService : IFeeVoucherService
    {
        private readonly IFeeVoucherRepository _repo;

        public FeeVoucherService(IFeeVoucherRepository repo)
        {
            _repo = repo;
        }

        public Task<FeeVoucherMain?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);
        public Task<ResponseDto> SaveAsync(FeeVoucherSaveDto dto, int userId, string userIp)
            => _repo.SaveAsync(dto, userId, userIp);
        public Task<string> GetNextVNoAsync() => _repo.GetNextVNoAsync();
        public Task<FeeVoucherReceiptDto?> GetReceiptAsync(decimal id)
             => _repo.GetReceiptAsync(id);
        public Task<List<FeeVoucherListDto>> GetAllAsync(int? classId, int? sectionId, int? studentId, DateTime dateFrom, DateTime dateTo)
             => _repo.GetAllAsync(classId, sectionId, studentId, dateFrom, dateTo);

        public Task<ResponseDto> DeleteByIdAsync(int id, int userId, string userIp)
            => _repo.DeleteByIdAsync(id, userId, userIp);

    }
}
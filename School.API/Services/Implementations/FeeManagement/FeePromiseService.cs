using School.API.DTOs.Common;
using School.API.DTOs.FeeManagement;
using School.API.Repositories.Interfaces.Accounts;
using School.API.Services.Interfaces.Accounts;

namespace School.API.Services.Implementations.Accounts
{
    public class FeePromiseService : IFeePromiseService
    {
        private readonly IFeePromiseRepository _repository;

        public FeePromiseService(IFeePromiseRepository repository)
        {
            _repository = repository;
        }

        public Task<List<FeePromiseDto>> GetAsync(int? vid, int? studentId, string? classIds, string? campusIds, byte status)
            => _repository.GetAsync(vid, studentId, classIds, campusIds, status);

        public Task<List<FeePromiseReportDto>> GetReportAsync(string? classIds, string? campusIds, byte status)
            => _repository.GetReportAsync(classIds, campusIds, status);

        public Task<ResponseDto?> SaveAsync(SaveFeePromiseDto dto, int userId, string userIp)
            => _repository.SaveAsync(dto, userId, userIp);

        public Task<ResponseDto?> DeleteAsync(int vid, int userId, string userIp)
            => _repository.DeleteAsync(vid, userId, userIp);
    }
}

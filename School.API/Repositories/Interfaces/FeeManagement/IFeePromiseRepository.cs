using School.API.DTOs.Common;
using School.API.DTOs.FeeManagement;

namespace School.API.Repositories.Interfaces.Accounts
{
    public interface IFeePromiseRepository
    {
        /// <summary>Promises, with each student's dues and the state of the promise.</summary>
        Task<List<FeePromiseDto>> GetAsync(int? vid, int? studentId, string? classIds, string? campusIds, byte status);

        Task<List<FeePromiseReportDto>> GetReportAsync(string? classIds, string? campusIds, byte status);

        Task<ResponseDto?> SaveAsync(SaveFeePromiseDto dto, int userId, string userIp);
        Task<ResponseDto?> DeleteAsync(int vid, int userId, string userIp);
    }
}

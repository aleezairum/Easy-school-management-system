using School.API.DTOs.Academic;
using School.API.DTOs.Common;

namespace School.API.Services.Interfaces.Academic
{
    public interface IClassTimingService
    {
        Task<List<ClassTimingDto>> GetAllAsync(int campusId = 0);

        Task<ClassTimingDto?> GetByIdAsync(int vid);

        Task<ResponseDto?> SaveAsync(SaveClassTimingDto dto, int userId, string userIp);

        /// <summary>Saves the whole grid; blank rows are skipped, not rejected.</summary>
        Task<SaveClassTimingResultDto> SaveBatchAsync(
            SaveClassTimingBatchDto batch, int userId, string userIp);

        Task<ResponseDto?> DeleteByIdAsync(int vid);
    }
}

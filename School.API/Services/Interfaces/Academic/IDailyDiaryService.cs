using School.API.Data.DBModels.Academic;
using School.API.DTOs.Academic;

namespace School.API.Services.Interfaces.Academic
{
    public interface IDailyDiaryService
    {
        Task<List<DailyDiaryDto>> GetAsync(DateTime? date, int? classId);

        /// <summary>Adds the pages to the day's diary for the class and section, making it if needed.</summary>
        /// <exception cref="InvalidOperationException">A bad image, or nothing to save.</exception>
        Task<DailyDiaryDto> SaveAsync(SaveDailyDiaryDto dto, int? userId);

        Task<SMSDailyDiaryImage?> GetImageAsync(int imageId);
        Task<bool> DeleteImageAsync(int imageId);
        Task<bool> DeleteAsync(int diaryId);
    }
}

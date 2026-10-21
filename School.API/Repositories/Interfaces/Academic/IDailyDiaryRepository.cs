using School.API.Data.DBModels.Academic;
using School.API.DTOs.Academic;

namespace School.API.Repositories.Interfaces.Academic
{
    public interface IDailyDiaryRepository
    {
        /// <summary>Diaries for a date (all dates when null), optionally one class, with their image list.</summary>
        Task<List<DailyDiaryDto>> GetAsync(DateTime? date, int? classId);

        /// <summary>The diary for this date, class and section (null = all sections), tracked, with images.</summary>
        Task<SMSDailyDiary?> FindAsync(DateTime date, int classId, int? sectionId);

        Task<SMSDailyDiary> AddAsync(SMSDailyDiary diary);
        Task SaveChangesAsync();

        Task<SMSDailyDiaryImage?> GetImageAsync(int imageId);

        /// <summary>Removes one page; the diary goes too when it is left with no pages and no notes.</summary>
        Task<bool> DeleteImageAsync(int imageId);

        Task<bool> DeleteAsync(int diaryId);
    }
}

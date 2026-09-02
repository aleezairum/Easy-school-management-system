using School.API.DTOs.HR;

namespace School.API.Services.Interfaces.HR
{
    public interface ISchoolTimingService
    {
        Task<List<SchoolTimingDto>> GetAllAsync(int? year = null);
        Task<SchoolTimingDto?> GetByIdAsync(int id);
        Task<List<SchoolTimingDayDto>> GetDaysAsync(int timingId);

        /// <summary>
        /// The individual days a range works out to, WITHOUT saving anything —
        /// holidays dropped, Fridays on the Friday times. This is what the screen
        /// lists for editing before the user commits to it.
        /// </summary>
        Task<List<SchoolTimingDayDto>> ExpandAsync(
            DateTime startDate, DateTime endDate,
            TimeSpan timeIn, TimeSpan timeOut,
            TimeSpan? fridayTimeIn, TimeSpan? fridayTimeOut,
            byte? offDay1, byte? offDay2);

        // Saving writes one row per day. The screen sends the day list it showed;
        // an API caller can send none and let the range be expanded instead.
        Task<SchoolTimingDto> CreateAsync(SaveSchoolTimingDto dto, int userId);
        Task<SchoolTimingDto?> UpdateAsync(int id, SaveSchoolTimingDto dto, int userId);
        Task<bool> DeleteAsync(int id);

        // One already-saved day: change it, or put it back on what the range says.
        Task<SchoolTimingDayDto?> UpdateDayAsync(int dayId, SaveSchoolTimingDayTimeDto dto);
        Task<SchoolTimingDayDto?> ResetDayAsync(int dayId);
    }
}

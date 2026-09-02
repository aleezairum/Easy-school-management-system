using School.API.Data.DBModels.HR;
using School.API.DTOs.HR;

namespace School.API.Repositories.Interfaces.HR
{
    public interface ISchoolTimingRepository
    {
        Task<List<SchoolTimingDto>> GetAllAsync(int? year = null);
        Task<SchoolTimingDto?> GetByIdAsync(int id);

        /// <summary>The days a range resolved to, in date order.</summary>
        Task<List<SchoolTimingDayDto>> GetDaysAsync(int timingId);

        Task<HrSchoolTiming> CreateAsync(SaveSchoolTimingDto dto, int userId);
        Task<HrSchoolTiming?> UpdateAsync(int id, SaveSchoolTimingDto dto, int userId);
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Rewrites this timing's day rows from <paramref name="plan"/>, one row
        /// per date. Rows on those dates belonging to an older range are dropped —
        /// a date answers to exactly one timing. Days edited by hand
        /// (IsOverridden) are kept as they are, provided the range still covers
        /// them.
        /// </summary>
        Task<int> ApplyDaysAsync(int timingId, IReadOnlyCollection<SchoolTimingDayPlan> plan, bool preserveEdited);

        /// <summary>Edits one day and marks it as hand-edited.</summary>
        Task<SchoolTimingDayDto?> UpdateDayAsync(int dayId, SaveSchoolTimingDayTimeDto dto);

        /// <summary>
        /// Puts one hand-edited day back on the times its range would give it.
        /// </summary>
        Task<SchoolTimingDayDto?> ResetDayAsync(int dayId, TimeSpan timeIn, TimeSpan timeOut);

        Task<HrSchoolTimingDay?> GetDayAsync(int dayId);

        /// <summary>What attendance reads: the school timing per date, if any.</summary>
        Task<Dictionary<DateTime, HrSchoolTimingDay>> GetDaysByDateAsync(DateTime from, DateTime to);
    }
}

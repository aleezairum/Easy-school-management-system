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

        /// <summary>
        /// What attendance reads: the school timing per date AND designation.
        /// </summary>
        /// <remarks>
        /// Keyed by (date, designation) because a date no longer resolves to one
        /// timing — a Principal and a Teacher keep different hours on the same
        /// day. A null designation in the key is the school-wide row, which is
        /// what the caller falls back to when a designation has none of its own.
        /// </remarks>
        Task<Dictionary<(DateTime Date, int? DesignationId), HrSchoolTimingDay>> GetDaysByDateAsync(
            DateTime from, DateTime to);

        /// <summary>
        /// The weekly off days in force over a period, keyed by designation id,
        /// with <b>0 meaning the school-wide range</b>.
        /// </summary>
        /// <remarks>
        /// 0 rather than null because Dictionary rejects a null key outright,
        /// even when the key type is int?. Designation ids start at 1, so 0 is
        /// free to mean "applies to everyone".
        ///
        /// These used to come from the employee's shift. They live on the timing
        /// range now, and the range is chosen the same way its days are: the
        /// designation's own if it has one, otherwise the school-wide row.
        /// </remarks>
        Task<Dictionary<int, (byte? OffDay1, byte? OffDay2)>> GetOffDaysAsync(DateTime from, DateTime to);
    }
}

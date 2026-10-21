// Repositories/Interfaces/IWaBotRepository.cs
using School.API.DTOs.SMS;

namespace School.API.Repositories.Interfaces
{
    /// <summary>
    /// Read-only lookups behind the WhatsApp auto-reply. Deliberately separate
    /// from IWaChatRepository: that one owns the conversation/message tables,
    /// this one only reads student, fee, result and attendance data.
    /// </summary>
    public interface IWaBotRepository
    {
        /// <summary>
        /// Every active student reachable on this WhatsApp number — the student's
        /// own mobile, or the father's, mother's or guardian's. A parent with
        /// three children at the school gets three rows.
        /// </summary>
        Task<List<BotStudentDto>> GetStudentsByPhoneAsync(string waId);

        /// <summary>
        /// Active staff whose own mobile or phone is this WhatsApp number. One
        /// number can be both a parent's and a member of staff's.
        /// </summary>
        Task<List<BotStaffDto>> GetStaffByPhoneAsync(string waId);

        /// <summary>
        /// The newest exam with marks in a subject assigned to this teacher,
        /// or null for staff who teach nothing (or nothing examined yet).
        /// </summary>
        Task<(int ExamId, string ExamName)?> GetTeacherLatestExamAsync(int employeeId);

        /// <summary>
        /// The pages of the diary for this class on this date: the section's
        /// own diary and the class-wide one (no section), whichever exist.
        /// </summary>
        Task<List<BotDiaryPageDto>> GetDiaryPagesAsync(int classId, int sectionId, DateTime date);

        /// <summary>
        /// Announcements running today for this audience (1 parents, 2 staff)
        /// on these campuses - those for everyone and for both campuses included.
        /// </summary>
        Task<List<BotAnnouncementDto>> GetCurrentAnnouncementsAsync(byte audience, IEnumerable<int?> campusIds);

        Task<List<BotDueDto>> GetPendingDuesAsync(int studentId);

        Task<BotVoucherDto?> GetLastVoucherAsync(int studentId);

        Task<BotLedgerSummaryDto?> GetLedgerSummaryAsync(int studentId);

        Task<BotAttendanceDto?> GetAttendanceSummaryAsync(int studentId, DateTime from, DateTime to);

        /// <summary>Today's single mark, or null when the register is not filled.</summary>
        Task<BotTodayAttendanceDto?> GetTodayAttendanceAsync(int studentId, DateTime date);

        Task<List<BotResultDto>> GetLatestResultAsync(int studentId);

        // ── Enquiry lookups ─────────────────────────────────────────────
        // These read the school's own records rather than figures typed into
        // config and left to go stale.

        /// <summary>
        /// Every class's student timing, campus and class included, so the
        /// caller can pick the rows matching the children on the number.
        /// </summary>
        /// <remarks>
        /// From SMSClassTiming — the Class Timings screen — and NOT from
        /// HrSchoolTiming, which describes the staff working day for attendance
        /// and payroll. Empty when nothing has been entered yet, or when the
        /// table does not exist on this database.
        /// </remarks>
        Task<List<BotClassTimingDto>> GetClassTimingsAsync();

        /// <summary>
        /// The current session's fee schedule, campus by campus — the same
        /// figures the fee is actually charged on.
        /// </summary>
        Task<BotFeeStructureDto?> GetFeeStructureAsync();
    }
}

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

        Task<List<BotDueDto>> GetPendingDuesAsync(int studentId);

        Task<BotVoucherDto?> GetLastVoucherAsync(int studentId);

        Task<BotLedgerSummaryDto?> GetLedgerSummaryAsync(int studentId);

        Task<BotAttendanceDto?> GetAttendanceSummaryAsync(int studentId, DateTime from, DateTime to);

        Task<List<BotResultDto>> GetLatestResultAsync(int studentId);
    }
}

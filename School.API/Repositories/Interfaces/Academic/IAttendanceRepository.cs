using School.API.Data.DBModels.Academic;
using School.API.DTOs;

namespace School.API.Repositories.Interfaces.Academic
{
    public interface IAttendanceRepository
    {
        Task<IEnumerable<AttendanceListDto>> GetAllAsync();
        Task<AttendanceDto?> GetByIdAsync(int id);
        Task<IEnumerable<AttendanceListDto>> GetByDateAsync(DateTime date);
        Task<IEnumerable<AttendanceListDto>> GetByClassSectionDateAsync(int classId, int sectionId, int sessionId, DateTime date);
        Task<IEnumerable<AttendanceListDto>> GetByStudentAsync(int studentId, int sessionId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<Attendance> CreateAsync(CreateAttendanceDto dto);
        Task<Attendance?> UpdateAsync(int id, UpdateAttendanceDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> AttendanceExistsAsync(int studentId, DateTime date);
        Task SaveBulkAttendanceAsync(BulkAttendanceDto dto);
        Task<IEnumerable<AttendanceSummaryDto>> GetAttendanceSummaryAsync(int classId, int sectionId, int sessionId, DateTime fromDate, DateTime toDate);
    }
}

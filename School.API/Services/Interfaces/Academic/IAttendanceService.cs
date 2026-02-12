using School.API.DTOs;

namespace School.API.Services.Interfaces.Academic
{
    public interface IAttendanceService
    {
        Task<IEnumerable<AttendanceListDto>> GetAllAsync();
        Task<AttendanceDto?> GetByIdAsync(int id);
        Task<IEnumerable<AttendanceListDto>> GetByDateAsync(DateTime date);
        Task<IEnumerable<AttendanceListDto>> GetByClassSectionDateAsync(int classId, int sectionId, int sessionId, DateTime date);
        Task<IEnumerable<AttendanceListDto>> GetByStudentAsync(int studentId, int sessionId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<AttendanceDto> CreateAsync(CreateAttendanceDto dto);
        Task<AttendanceDto?> UpdateAsync(int id, UpdateAttendanceDto dto);
        Task<bool> DeleteAsync(int id);
        Task SaveBulkAttendanceAsync(BulkAttendanceDto dto);
        Task<IEnumerable<AttendanceSummaryDto>> GetAttendanceSummaryAsync(int classId, int sectionId, int sessionId, DateTime fromDate, DateTime toDate);
    }
}

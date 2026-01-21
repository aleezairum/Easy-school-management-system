using School.API.Data.DBModels.Academic;
using School.API.DTOs;

namespace School.API.Repositories.Interfaces.Academic
{
    public interface ITimeTableRepository
    {
        Task<IEnumerable<TimeTableListDto>> GetAllAsync();
        Task<TimeTableDto?> GetByIdAsync(int id);
        Task<IEnumerable<TimeTableListDto>> GetByClassSectionAsync(int classId, int sectionId, int sessionId);
        Task<IEnumerable<TimeTableListDto>> GetByTeacherAsync(int teacherId, int sessionId);
        Task<TimeTable> CreateAsync(CreateTimeTableDto dto);
        Task<TimeTable?> UpdateAsync(int id, UpdateTimeTableDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> HasConflictAsync(int classId, int sectionId, DayOfWeek dayOfWeek, int periodNumber, int sessionId, int? excludeId = null);
    }
}

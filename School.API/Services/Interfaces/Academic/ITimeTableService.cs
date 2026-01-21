using School.API.DTOs;

namespace School.API.Services.Interfaces.Academic
{
    public interface ITimeTableService
    {
        Task<IEnumerable<TimeTableListDto>> GetAllAsync();
        Task<TimeTableDto?> GetByIdAsync(int id);
        Task<IEnumerable<TimeTableListDto>> GetByClassSectionAsync(int classId, int sectionId, int sessionId);
        Task<IEnumerable<TimeTableListDto>> GetByTeacherAsync(int teacherId, int sessionId);
        Task<TimeTableDto> CreateAsync(CreateTimeTableDto dto);
        Task<TimeTableDto?> UpdateAsync(int id, UpdateTimeTableDto dto);
        Task<bool> DeleteAsync(int id);
    }
}

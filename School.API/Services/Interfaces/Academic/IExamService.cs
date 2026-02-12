using School.API.DTOs;

namespace School.API.Services.Interfaces.Academic
{
    public interface IExamService
    {
        // Exam methods
        Task<IEnumerable<ExamListDto>> GetAllAsync();
        Task<ExamDto?> GetByIdAsync(int id);
        Task<IEnumerable<ExamListDto>> GetByClassAsync(int classId, int sessionId);
        Task<IEnumerable<ExamDropdownDto>> GetDropdownAsync(int? classId = null, int? sessionId = null);
        Task<ExamDto> CreateAsync(CreateExamDto dto);
        Task<ExamDto?> UpdateAsync(int id, UpdateExamDto dto);
        Task<bool> DeleteAsync(int id);

        // Exam Result methods
        Task<IEnumerable<ExamResultListDto>> GetResultsByExamAsync(int examId);
        Task<ExamResultDto?> GetResultByIdAsync(int id);
        Task<ExamResultDto> CreateResultAsync(CreateExamResultDto dto);
        Task<bool> DeleteResultAsync(int id);
        Task SaveBulkResultsAsync(BulkExamResultDto dto);
    }
}

using School.API.Data.DBModels.Academic;
using School.API.DTOs;

namespace School.API.Repositories.Interfaces.Academic
{
    public interface IExamRepository
    {
        // Exam methods
        Task<IEnumerable<ExamListDto>> GetAllAsync();
        Task<ExamDto?> GetByIdAsync(int id);
        Task<IEnumerable<ExamListDto>> GetByClassAsync(int classId, int sessionId);
        Task<IEnumerable<ExamDropdownDto>> GetDropdownAsync(int? classId = null, int? sessionId = null);
        Task<Exam> CreateAsync(CreateExamDto dto);
        Task<Exam?> UpdateAsync(int id, UpdateExamDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);

        // Exam Result methods
        Task<IEnumerable<ExamResultListDto>> GetResultsByExamAsync(int examId);
        Task<ExamResultDto?> GetResultByIdAsync(int id);
        Task<ExamResult> CreateResultAsync(CreateExamResultDto dto);
        Task<bool> DeleteResultAsync(int id);
        Task SaveBulkResultsAsync(BulkExamResultDto dto);
    }
}

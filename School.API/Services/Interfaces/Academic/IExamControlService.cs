using School.API.DTOs.Academic;

namespace School.API.Services.Interfaces.Academic
{
    public interface IExamControlService
    {
        Task<List<ExamFinalizeDto>> GetFinalizedAsync();
        Task<bool> IsFinalizedAsync(int examId);
        Task FinalizeAsync(int examId, int? userId);
        Task UnfinalizeAsync(int examId);
        Task<List<ExamExcludeRowDto>> GetExcludeListAsync(int examId, int? classId, int? sectionId);
        Task<int> SaveExcludeListAsync(SaveExamExcludeDto dto, int? userId);
    }
}

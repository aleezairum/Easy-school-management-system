using School.API.DTOs.Academic;

namespace School.API.Repositories.Interfaces.Academic
{
    public interface IExamControlRepository
    {
        Task<List<ExamFinalizeDto>> GetFinalizedAsync();
        Task<bool> IsFinalizedAsync(int examId);
        Task FinalizeAsync(int examId, int? userId);
        Task UnfinalizeAsync(int examId);

        /// <param name="classId">Null: only the students already excluded, from every class.</param>
        Task<List<ExamExcludeRowDto>> GetExcludeListAsync(int examId, int? classId, int? sectionId);
        Task<int> SaveExcludeListAsync(SaveExamExcludeDto dto, int? userId);
    }
}

using School.API.DTOs.Academic;
using School.API.Repositories.Interfaces.Academic;
using School.API.Services.Interfaces.Academic;

namespace School.API.Services.Implementations.Academic
{
    public class ExamControlService : IExamControlService
    {
        private readonly IExamControlRepository _repository;

        public ExamControlService(IExamControlRepository repository)
        {
            _repository = repository;
        }

        public Task<List<ExamFinalizeDto>> GetFinalizedAsync() => _repository.GetFinalizedAsync();

        public Task<bool> IsFinalizedAsync(int examId) => _repository.IsFinalizedAsync(examId);

        public Task FinalizeAsync(int examId, int? userId) => _repository.FinalizeAsync(examId, userId);

        public Task UnfinalizeAsync(int examId) => _repository.UnfinalizeAsync(examId);

        public Task<List<ExamExcludeRowDto>> GetExcludeListAsync(int examId, int? classId, int? sectionId) =>
            _repository.GetExcludeListAsync(examId, classId, sectionId);

        public Task<int> SaveExcludeListAsync(SaveExamExcludeDto dto, int? userId)
        {
            if (dto.ExamId <= 0) throw new InvalidOperationException("Choose the exam.");
            if (dto.Items.Count == 0) throw new InvalidOperationException("No students to save.");
            return _repository.SaveExcludeListAsync(dto, userId);
        }
    }
}

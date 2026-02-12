using School.API.DTOs;
using School.API.Repositories.Interfaces.Academic;
using School.API.Services.Interfaces.Academic;

namespace School.API.Services.Implementations.Academic
{
    public class ExamService : IExamService
    {
        private readonly IExamRepository _repository;

        public ExamService(IExamRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<ExamListDto>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<ExamDto?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<ExamListDto>> GetByClassAsync(int classId, int sessionId)
        {
            return await _repository.GetByClassAsync(classId, sessionId);
        }

        public async Task<IEnumerable<ExamDropdownDto>> GetDropdownAsync(int? classId = null, int? sessionId = null)
        {
            return await _repository.GetDropdownAsync(classId, sessionId);
        }

        public async Task<ExamDto> CreateAsync(CreateExamDto dto)
        {
            var entity = await _repository.CreateAsync(dto);
            var result = await _repository.GetByIdAsync(entity.Id);
            return result!;
        }

        public async Task<ExamDto?> UpdateAsync(int id, UpdateExamDto dto)
        {
            var entity = await _repository.UpdateAsync(id, dto);
            if (entity == null)
                return null;

            return await _repository.GetByIdAsync(id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        // Exam Result methods
        public async Task<IEnumerable<ExamResultListDto>> GetResultsByExamAsync(int examId)
        {
            return await _repository.GetResultsByExamAsync(examId);
        }

        public async Task<ExamResultDto?> GetResultByIdAsync(int id)
        {
            return await _repository.GetResultByIdAsync(id);
        }

        public async Task<ExamResultDto> CreateResultAsync(CreateExamResultDto dto)
        {
            var entity = await _repository.CreateResultAsync(dto);
            var result = await _repository.GetResultByIdAsync(entity.Id);
            return result!;
        }

        public async Task<bool> DeleteResultAsync(int id)
        {
            return await _repository.DeleteResultAsync(id);
        }

        public async Task SaveBulkResultsAsync(BulkExamResultDto dto)
        {
            await _repository.SaveBulkResultsAsync(dto);
        }
    }
}

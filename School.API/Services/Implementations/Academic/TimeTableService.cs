using School.API.DTOs;
using School.API.Repositories.Interfaces.Academic;
using School.API.Services.Interfaces.Academic;

namespace School.API.Services.Implementations.Academic
{
    public class TimeTableService : ITimeTableService
    {
        private readonly ITimeTableRepository _repository;

        public TimeTableService(ITimeTableRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<TimeTableListDto>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<TimeTableDto?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<TimeTableListDto>> GetByClassSectionAsync(int classId, int sectionId, int sessionId)
        {
            return await _repository.GetByClassSectionAsync(classId, sectionId, sessionId);
        }

        public async Task<IEnumerable<TimeTableListDto>> GetByTeacherAsync(int teacherId, int sessionId)
        {
            return await _repository.GetByTeacherAsync(teacherId, sessionId);
        }

        public async Task<TimeTableDto> CreateAsync(CreateTimeTableDto dto)
        {
            var entity = await _repository.CreateAsync(dto);
            var result = await _repository.GetByIdAsync(entity.Id);
            return result!;
        }

        public async Task<TimeTableDto?> UpdateAsync(int id, UpdateTimeTableDto dto)
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
    }
}

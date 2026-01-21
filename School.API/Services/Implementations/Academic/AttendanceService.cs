using School.API.DTOs;
using School.API.Repositories.Interfaces.Academic;
using School.API.Services.Interfaces.Academic;

namespace School.API.Services.Implementations.Academic
{
    public class AttendanceService : IAttendanceService
    {
        private readonly IAttendanceRepository _repository;

        public AttendanceService(IAttendanceRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<AttendanceListDto>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<AttendanceDto?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<AttendanceListDto>> GetByDateAsync(DateTime date)
        {
            return await _repository.GetByDateAsync(date);
        }

        public async Task<IEnumerable<AttendanceListDto>> GetByClassSectionDateAsync(int classId, int sectionId, int sessionId, DateTime date)
        {
            return await _repository.GetByClassSectionDateAsync(classId, sectionId, sessionId, date);
        }

        public async Task<IEnumerable<AttendanceListDto>> GetByStudentAsync(int studentId, int sessionId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            return await _repository.GetByStudentAsync(studentId, sessionId, fromDate, toDate);
        }

        public async Task<AttendanceDto> CreateAsync(CreateAttendanceDto dto)
        {
            var entity = await _repository.CreateAsync(dto);
            var result = await _repository.GetByIdAsync(entity.Id);
            return result!;
        }

        public async Task<AttendanceDto?> UpdateAsync(int id, UpdateAttendanceDto dto)
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

        public async Task SaveBulkAttendanceAsync(BulkAttendanceDto dto)
        {
            await _repository.SaveBulkAttendanceAsync(dto);
        }

        public async Task<IEnumerable<AttendanceSummaryDto>> GetAttendanceSummaryAsync(int classId, int sectionId, int sessionId, DateTime fromDate, DateTime toDate)
        {
            return await _repository.GetAttendanceSummaryAsync(classId, sectionId, sessionId, fromDate, toDate);
        }
    }
}

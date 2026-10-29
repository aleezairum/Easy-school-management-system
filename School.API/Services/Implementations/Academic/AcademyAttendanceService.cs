using School.API.DTOs.Academic;
using School.API.Repositories.Interfaces.Academic;
using School.API.Services.Interfaces.Academic;

namespace School.API.Services.Implementations.Academic
{
    public class AcademyAttendanceService : IAcademyAttendanceService
    {
        private readonly IAcademyAttendanceRepository _repo;

        public AcademyAttendanceService(IAcademyAttendanceRepository repo)
        {
            _repo = repo;
        }

        public Task<List<AcademyAttendanceRowDto>> GetAsync(int classId, int? sectionId, DateTime date)
            => _repo.GetAsync(classId, sectionId, date);

        public Task<int> SaveAsync(SaveAcademyAttendanceDto dto, int? userId)
        {
            if (dto.Date == default) throw new InvalidOperationException("Choose the date.");
            if (dto.Date.Date > DateTime.Today) throw new InvalidOperationException("Attendance cannot be marked for a day still to come.");
            if (dto.Items.Count == 0) throw new InvalidOperationException("Nobody to save.");
            return _repo.SaveAsync(dto, userId);
        }

        public Task<int> DeleteAsync(int classId, int? sectionId, DateTime date)
            => _repo.DeleteAsync(classId, sectionId, date);
    }
}

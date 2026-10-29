using School.API.DTOs.Academic;

namespace School.API.Services.Interfaces.Academic
{
    /// <summary>Academy (after-school) attendance of the students with "Avail Academy" on.</summary>
    public interface IAcademyAttendanceService
    {
        Task<List<AcademyAttendanceRowDto>> GetAsync(int classId, int? sectionId, DateTime date);

        /// <exception cref="InvalidOperationException">No date, or nothing to save.</exception>
        Task<int> SaveAsync(SaveAcademyAttendanceDto dto, int? userId);
        Task<int> DeleteAsync(int classId, int? sectionId, DateTime date);
    }
}

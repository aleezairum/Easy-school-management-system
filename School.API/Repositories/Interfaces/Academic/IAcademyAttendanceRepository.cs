using School.API.DTOs.Academic;

namespace School.API.Repositories.Interfaces.Academic
{
    public interface IAcademyAttendanceRepository
    {
        /// <summary>Enrolled students with "Avail Academy" on, in a class (and section), with their mark for the day.</summary>
        Task<List<AcademyAttendanceRowDto>> GetAsync(int classId, int? sectionId, DateTime date);

        /// <summary>Saves the day's marks (one per student, replacing any earlier one). Returns how many were saved.</summary>
        Task<int> SaveAsync(SaveAcademyAttendanceDto dto, int? userId);

        /// <summary>Removes the day's marks of a class (and section). Returns how many were removed.</summary>
        Task<int> DeleteAsync(int classId, int? sectionId, DateTime date);
    }
}

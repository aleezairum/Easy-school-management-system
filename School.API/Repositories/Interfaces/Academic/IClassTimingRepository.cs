using School.API.DTOs.Academic;
using School.API.DTOs.Common;

namespace School.API.Repositories.Interfaces.Academic
{
    /// <summary>
    /// Class-wise school timing for students — see Scripts/SMSClassTiming.sql.
    /// Not to be confused with ISchoolTimingRepository, which owns the STAFF
    /// timing calendar that attendance and payroll read.
    /// </summary>
    public interface IClassTimingRepository
    {
        /// <summary>
        /// Every active class on a campus with its timing, blanks included, so
        /// the editing grid can show what still needs filling in. Pass campusId
        /// 0 for the saved rows only, across all campuses.
        /// </summary>
        Task<List<ClassTimingDto>> GetAllAsync(int campusId = 0);

        Task<ClassTimingDto?> GetByIdAsync(int vid);

        Task<ResponseDto?> SaveAsync(SaveClassTimingDto dto, int userId, string userIp);

        Task<ResponseDto?> DeleteByIdAsync(int vid);
    }
}

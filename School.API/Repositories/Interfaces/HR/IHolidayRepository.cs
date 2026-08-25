using School.API.Data.DBModels.HR;
using School.API.DTOs.HR;

namespace School.API.Repositories.Interfaces.HR
{
    public interface IHolidayRepository
    {
        Task<List<HolidayDto>> GetAllAsync(int? year = null);
        Task<HolidayDto?> GetByIdAsync(int id);
        Task<HrHoliday> CreateAsync(SaveHolidayDto dto);
        Task<HrHoliday?> UpdateAsync(int id, SaveHolidayDto dto);
        Task<bool> DeleteAsync(int id);
        Task<int> CopyYearAsync(int fromYear, int toYear);
        Task<HashSet<DateTime>> GetActiveDatesAsync(DateTime from, DateTime to);
        Task<Dictionary<DateTime, string>> GetActiveNamesAsync(DateTime from, DateTime to);
    }
}

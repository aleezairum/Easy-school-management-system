using School.API.DTOs.HR;

namespace School.API.Services.Interfaces.HR
{
    public interface IHolidayService
    {
        Task<List<HolidayDto>> GetAllAsync(int? year = null);
        Task<HolidayDto?> GetByIdAsync(int id);
        Task<HolidayDto> CreateAsync(SaveHolidayDto dto);
        Task<HolidayDto?> UpdateAsync(int id, SaveHolidayDto dto);
        Task<bool> DeleteAsync(int id);
        Task<int> CopyYearAsync(CopyHolidayYearDto dto);
    }
}

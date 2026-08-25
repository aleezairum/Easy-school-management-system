using School.API.DTOs.HR;

namespace School.API.Services.Interfaces.HR
{
    public interface IShiftService
    {
        Task<List<ShiftDto>> GetAllAsync();
        Task<ShiftDto?> GetByIdAsync(int id);
        Task<List<ShiftDropdownDto>> GetDropdownAsync();
        Task<ShiftDto> CreateAsync(SaveShiftDto dto);
        Task<ShiftDto?> UpdateAsync(int id, SaveShiftDto dto);
        Task<bool> DeleteAsync(int id);
    }
}

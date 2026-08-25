using School.API.Data.DBModels.HR;
using School.API.DTOs.HR;

namespace School.API.Repositories.Interfaces.HR
{
    public interface IShiftRepository
    {
        Task<List<ShiftDto>> GetAllAsync();
        Task<ShiftDto?> GetByIdAsync(int id);
        Task<List<ShiftDropdownDto>> GetDropdownAsync();
        Task<HrShift?> GetEntityByIdAsync(int id);
        Task<HrShift> CreateAsync(SaveShiftDto dto);
        Task<HrShift?> UpdateAsync(int id, SaveShiftDto dto);
        Task<bool> DeleteAsync(int id);
    }
}

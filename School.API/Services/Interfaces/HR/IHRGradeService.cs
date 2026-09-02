using School.API.DTOs;

namespace School.API.Services.Interfaces.HR
{
    public interface IHRGradeService
    {
        Task<IEnumerable<HRGradeListDto>> GetAllAsync();
        Task<HRGradeDto?> GetByIdAsync(int id);
        Task<HRGradeDto> CreateAsync(CreateHRGradeDto dto);
        Task<HRGradeDto?> UpdateAsync(int id, UpdateHRGradeDto dto);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<HRGradeDropdownDto>> GetDropdownAsync();
        Task<bool> CodeExistsAsync(string code, int? excludeId = null);
        Task<bool> IsInUseAsync(int id);
    }
}

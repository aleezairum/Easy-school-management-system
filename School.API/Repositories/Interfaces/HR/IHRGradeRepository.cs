using School.API.Data.DBModels.HR;
using School.API.DTOs;

namespace School.API.Repositories.Interfaces.HR
{
    public interface IHRGradeRepository
    {
        Task<IEnumerable<HRGradeListDto>> GetAllAsync();
        Task<HRGradeDto?> GetByIdAsync(int id);
        Task<HRGrade> CreateAsync(CreateHRGradeDto dto);
        Task<HRGrade?> UpdateAsync(int id, UpdateHRGradeDto dto);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<HRGradeDropdownDto>> GetDropdownAsync();
        Task<bool> ExistsAsync(int id);
        Task<bool> CodeExistsAsync(string code, int? excludeId = null);
    }
}

using School.API.Data.DBModels.HR;
using School.API.DTOs;

namespace School.API.Repositories.Interfaces.HR
{
    public interface IAcademicGradeRepository
    {
        Task<IEnumerable<AcademicGradeListDto>> GetAllAsync();
        Task<AcademicGradeDto?> GetByIdAsync(int id);
        Task<AcademicGrade> CreateAsync(CreateAcademicGradeDto dto);
        Task<AcademicGrade?> UpdateAsync(int id, UpdateAcademicGradeDto dto);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<AcademicGradeDropdownDto>> GetDropdownAsync();
        Task<bool> ExistsAsync(int id);
        Task<bool> CodeExistsAsync(string code, int? excludeId = null);
    }
}

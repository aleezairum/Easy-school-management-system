using School.API.DTOs;

namespace School.API.Services.Interfaces.HR
{
    public interface IAcademicGradeService
    {
        Task<IEnumerable<AcademicGradeListDto>> GetAllAsync();
        Task<AcademicGradeDto?> GetByIdAsync(int id);
        Task<AcademicGradeDto> CreateAsync(CreateAcademicGradeDto dto);
        Task<AcademicGradeDto?> UpdateAsync(int id, UpdateAcademicGradeDto dto);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<AcademicGradeDropdownDto>> GetDropdownAsync();
    }
}

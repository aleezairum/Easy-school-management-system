using School.API.DTOs;

namespace School.API.Services.Interfaces.HR
{
    public interface IDesignationService
    {
        Task<IEnumerable<DesignationListDto>> GetAllAsync();
        Task<DesignationDto?> GetByIdAsync(int id);
        Task<DesignationDto> CreateAsync(CreateDesignationDto dto);
        Task<DesignationDto?> UpdateAsync(int id, UpdateDesignationDto dto);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<DesignationDropdownDto>> GetDropdownAsync();
    }
}

using School.API.Data.DBModels.HR;
using School.API.DTOs;

namespace School.API.Repositories.Interfaces.HR
{
    public interface IDesignationRepository
    {
        Task<IEnumerable<DesignationListDto>> GetAllAsync();
        Task<DesignationDto?> GetByIdAsync(int id);
        Task<Designation> CreateAsync(CreateDesignationDto dto);
        Task<Designation?> UpdateAsync(int id, UpdateDesignationDto dto);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<DesignationDropdownDto>> GetDropdownAsync();
        Task<bool> ExistsAsync(int id);
        Task<bool> NameExistsAsync(string name, int? excludeId = null);
    }
}

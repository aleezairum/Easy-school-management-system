using School.API.Data.DBModels.HR;
using School.API.DTOs.HR;

namespace School.API.Repositories.Interfaces.HR
{
    public interface IEmployeeRepository
    {
        Task<List<EmployeeListDto>> GetAllAsync();
        Task<EmployeeDto?> GetByIdAsync(int id);
        Task<List<EmployeeDropdownDto>> GetDropdownAsync();
        Task<Employee> CreateAsync(SaveEmployeeDto dto);
        Task<Employee?> UpdateAsync(int id, SaveEmployeeDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> CodeExistsAsync(string code, int? excludeId = null);
    }
}

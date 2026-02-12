using School.API.Data.DBModels.HR;
using School.API.DTOs;

namespace School.API.Repositories.Interfaces.HR
{
    public interface IEmployeeRepository
    {
        Task<IEnumerable<EmployeeListDto>> GetAllAsync();
        Task<EmployeeDto?> GetByIdAsync(int id);
        Task<Employee> CreateAsync(CreateEmployeeDto dto);
        Task<Employee?> UpdateAsync(int id, UpdateEmployeeDto dto);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<EmployeeDropdownDto>> GetDropdownAsync();
        Task<bool> ExistsAsync(int id);
        Task<bool> EmployeeCodeExistsAsync(string employeeCode, int? excludeId = null);
        Task<bool> CNICExistsAsync(string cnic, int? excludeId = null);
    }
}

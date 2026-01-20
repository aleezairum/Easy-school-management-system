using School.API.DTOs;

namespace School.API.Services.Interfaces.HR
{
    public interface IEmployeeService
    {
        Task<IEnumerable<EmployeeListDto>> GetAllAsync();
        Task<EmployeeDto?> GetByIdAsync(int id);
        Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto);
        Task<EmployeeDto?> UpdateAsync(int id, UpdateEmployeeDto dto);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<EmployeeDropdownDto>> GetDropdownAsync();
    }
}

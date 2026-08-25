using School.API.DTOs.HR;

namespace School.API.Services.Interfaces.HR
{
    public interface IEmployeeService
    {
        Task<List<EmployeeListDto>> GetAllAsync();
        Task<EmployeeDto?> GetByIdAsync(int id);
        Task<List<EmployeeDropdownDto>> GetDropdownAsync();
        Task<EmployeeDto> CreateAsync(SaveEmployeeDto dto);
        Task<EmployeeDto?> UpdateAsync(int id, SaveEmployeeDto dto);
        Task<bool> DeleteAsync(int id);
    }
}

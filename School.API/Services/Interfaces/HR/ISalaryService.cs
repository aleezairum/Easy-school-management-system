using School.API.DTOs;

namespace School.API.Services.Interfaces.HR
{
    public interface ISalaryService
    {
        Task<IEnumerable<SalaryListDto>> GetAllAsync();
        Task<SalaryDto?> GetByIdAsync(int id);
        Task<IEnumerable<SalaryListDto>> GetByMonthYearAsync(int month, int year);
        Task<IEnumerable<SalaryListDto>> GetByEmployeeAsync(int employeeId);
        Task<SalaryDto> CreateAsync(CreateSalaryDto dto);
        Task<SalaryDto?> UpdateAsync(int id, UpdateSalaryDto dto);
        Task<bool> DeleteAsync(int id);
        Task ApproveSalariesAsync(List<int> salaryIds);
        Task PaySalaryAsync(PaySalaryDto dto);
    }
}

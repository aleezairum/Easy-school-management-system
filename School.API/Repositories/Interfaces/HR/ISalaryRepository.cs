using School.API.Data.DBModels.HR;
using School.API.DTOs;

namespace School.API.Repositories.Interfaces.HR
{
    public interface ISalaryRepository
    {
        Task<IEnumerable<SalaryListDto>> GetAllAsync();
        Task<SalaryDto?> GetByIdAsync(int id);
        Task<IEnumerable<SalaryListDto>> GetByMonthYearAsync(int month, int year);
        Task<IEnumerable<SalaryListDto>> GetByEmployeeAsync(int employeeId);
        Task<Salary> CreateAsync(CreateSalaryDto dto);
        Task<Salary?> UpdateAsync(int id, UpdateSalaryDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> SalaryExistsAsync(int employeeId, int month, int year);
        Task ApproveSalariesAsync(List<int> salaryIds);
        Task PaySalaryAsync(PaySalaryDto dto);
    }
}

using School.API.Data.DBModels.HR;
using School.API.DTOs.HR;

namespace School.API.Repositories.Interfaces.HR
{
    public interface IEmployeePayRepository
    {
        Task<List<EmployeePayDto>> GetHistoryAsync(int employeeId);
        Task<HrEmployeePay> CreateAsync(SaveEmployeePayDto dto);

        /// <summary>Latest pay structure with EffectiveFrom on or before the given date (5.2 step 1).</summary>
        Task<HrEmployeePay?> GetActiveAsOfAsync(int employeeId, DateTime asOf);

        /// <summary>Batch version for the payroll run's 4-query budget (section 9).</summary>
        Task<Dictionary<int, HrEmployeePay>> GetActiveAsOfForEmployeesAsync(IEnumerable<int> employeeIds, DateTime asOf);
    }
}

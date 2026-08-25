using School.API.DTOs.HR;

namespace School.API.Services.Interfaces.HR
{
    public interface IEmployeePayService
    {
        Task<List<EmployeePayDto>> GetHistoryAsync(int employeeId);
        Task<EmployeePayDto> CreateAsync(SaveEmployeePayDto dto);
    }
}

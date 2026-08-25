using School.API.DTOs.HR;

namespace School.API.Services.Interfaces.HR
{
    public interface IPayrollService
    {
        Task<List<PayrollRunDto>> GetRunsAsync();
        Task<PayrollRunResultDto> GenerateAsync(GeneratePayrollRunDto dto, int? userId);
        Task<List<PayslipDto>> GetPayslipsAsync(int runId);
        Task<PayslipDto?> GetPayslipAsync(int id);
        Task<bool> LockRunAsync(int runId, int? userId);
        Task<bool> PayEmployeeAsync(int payslipId, PayEmployeeDto dto);
        Task<bool> PayRunAsync(int runId, PayRunDto dto);
        Task<List<SalarySheetRowDto>> GetSalarySheetAsync(int runId);
    }
}

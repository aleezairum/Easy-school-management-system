using School.API.DTOs.HR;

namespace School.API.Services.Interfaces.HR
{
    public interface IPayrollService
    {
        Task<List<PayrollRunDto>> GetRunsAsync();
        Task<PayrollRunResultDto> GenerateAsync(GeneratePayrollRunDto dto, int? userId);
        Task<List<PayslipDto>> GetPayslipsAsync(int runId);
        Task<PayslipDto?> GetPayslipAsync(int id);
        Task<List<PayslipDto>> GetPayslipsForPeriodAsync(DateTime from, DateTime to, int? campusId, int? designationId, int? employeeId);

        /// <summary>How each employee's late, early and outing minutes are priced, per month in the range.</summary>
        Task<Dictionary<(int EmployeeId, int Year, int Month), School.API.Services.Implementations.HR.PayrollCalculator.DayMinutePricing>> GetMinutePricingAsync(
            DateTime from, DateTime to, ICollection<int> employeeIds);
        Task<bool> LockRunAsync(int runId, int? userId);
        Task<bool> DeleteRunAsync(int runId);
        Task<bool> PayEmployeeAsync(int payslipId, PayEmployeeDto dto);
        Task<bool> PayRunAsync(int runId, PayRunDto dto);
        Task<List<SalarySheetRowDto>> GetSalarySheetAsync(int runId);

        /// <summary>The month's hand-entered amounts, one row per employee.</summary>
        Task<List<MonthlyAmountDto>> GetMonthlyAmountsAsync(
            int year, byte month, int? campusId = null, List<int>? designationIds = null, string? sortBy = null);

        Task<int> SaveMonthlyAmountsAsync(SaveMonthlyAmountsDto dto, int? userId);

        /// <summary>Advances handed to staff, by date.</summary>
        Task<List<StaffAdvanceDto>> GetAdvancesAsync(DateTime from, DateTime to, int? employeeId, int? campusId);
        Task<int> SaveAdvanceAsync(int? id, SaveStaffAdvanceDto dto, int? userId);
        Task DeleteAdvanceAsync(int id);

        /// <summary>Who qualifies for extra days this month, and how many.</summary>
        Task<ExtraDaysSheetDto> GetExtraDaysAsync(int year, byte month, int? minWorkingDays,
            int? campusId = null, List<int>? designationIds = null, string? sortBy = null);

        Task<SaveExtraDaysResultDto> SaveExtraDaysAsync(SaveExtraDaysDto dto, int? userId);
    }
}

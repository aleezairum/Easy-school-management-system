using School.API.Data.DBModels.HR;
using School.API.DTOs.HR;

namespace School.API.Repositories.Interfaces.HR
{
    public class PayrollEmployeeRow
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int? DesignationId { get; set; }
        public string? DesignationName { get; set; }
        public int? HRGradeId { get; set; }
        public int? ShiftId { get; set; }
        public decimal ShiftWorkHours { get; set; } = 8;
        public byte? OffDay1 { get; set; }
        public byte? OffDay2 { get; set; }
        public DateTime? JoiningDate { get; set; }
        public DateTime? LeavingDate { get; set; }
        public HrEmployeePay? Pay { get; set; }
    }

    public interface IPayrollRepository
    {
        Task<List<PayrollRunDto>> GetRunsAsync();
        Task<HrPayrollRun?> GetRunEntityAsync(int id);
        Task<HrPayrollRun?> GetRunByPeriodAsync(int year, int month);
        Task<HrPayrollRun> CreateRunAsync(int year, int month, int? generatedBy);
        Task TouchRunGeneratedAsync(int runId, int? generatedBy);

        /// <summary>Replaces a Draft run's payslips in one SaveChanges call (one transaction) — section 9.</summary>
        Task RegeneratePayslipsAsync(int runId, List<HrPayslip> payslips);
        Task<List<PayslipDto>> GetPayslipsAsync(int runId);
        Task<PayslipDto?> GetPayslipByIdAsync(int id);
        Task<HrPayslip?> GetPayslipEntityByIdAsync(int id);
        Task<bool> SetRunStatusAsync(int id, HrPayrollRunStatus status, int? by);
        Task MarkPayslipPaidAsync(int id, PayEmployeeDto dto);
        Task MarkRunPaidAsync(int runId, PayRunDto dto);

        // The payroll run's 4-query budget (section 9):
        Task<List<PayrollEmployeeRow>> GetEmployeesForPayrollAsync(DateTime periodEnd);
        Task<List<HrAttendance>> GetAttendanceForPeriodAsync(IEnumerable<int> employeeIds, DateTime periodStart, DateTime periodEnd);
        Task<List<HrLeave>> GetApprovedLeaveForPeriodAsync(IEnumerable<int> employeeIds, DateTime periodStart, DateTime periodEnd);
        Task<List<HrPayComponent>> GetActiveComponentsAsync(DateTime periodStart, DateTime periodEnd);
    }
}

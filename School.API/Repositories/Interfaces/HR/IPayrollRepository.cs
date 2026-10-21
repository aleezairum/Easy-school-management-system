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
        public int? CampusId { get; set; }
        public string? CampusName { get; set; }
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
        /// <summary>Every run of the month: the all-campus one, or one per campus.</summary>
        Task<List<HrPayrollRun>> GetRunsForPeriodAsync(int year, int month);
        Task<HrPayrollRun> CreateRunAsync(int year, int month, int? campusId, int? generatedBy);

        /// <summary>Who already has a payslip in one of these runs, and in which.</summary>
        Task<Dictionary<int, int>> GetEmployeeRunsAsync(List<int> runIds);

        /// <summary>A Draft run and its payslips. False when it is not there.</summary>
        Task<bool> DeleteRunAsync(int runId);
        Task TouchRunGeneratedAsync(int runId, int? generatedBy);

        /// <summary>Replaces a Draft run's payslips in one SaveChanges call (one transaction) — section 9.</summary>
        Task RegeneratePayslipsAsync(int runId, List<HrPayslip> payslips);
        Task<List<PayslipDto>> GetPayslipsAsync(int runId);
        Task<PayslipDto?> GetPayslipByIdAsync(int id);
        Task<List<PayslipDto>> GetPayslipsForPeriodAsync(DateTime from, DateTime to, int? campusId, int? designationId, int? employeeId);
        Task<HrPayslip?> GetPayslipEntityByIdAsync(int id);
        Task<bool> SetRunStatusAsync(int id, HrPayrollRunStatus status, int? by);
        Task MarkPayslipPaidAsync(int id, PayEmployeeDto dto);
        Task MarkRunPaidAsync(int runId, PayRunDto dto);

        // The payroll run's 4-query budget (section 9):
        Task<List<PayrollEmployeeRow>> GetEmployeesForPayrollAsync(DateTime periodEnd);
        Task<List<HrAttendance>> GetAttendanceForPeriodAsync(IEnumerable<int> employeeIds, DateTime periodStart, DateTime periodEnd);
        Task<List<HrLeave>> GetApprovedLeaveForPeriodAsync(IEnumerable<int> employeeIds, DateTime periodStart, DateTime periodEnd);
        Task<List<HrPayComponent>> GetActiveComponentsAsync(DateTime periodStart, DateTime periodEnd);

        /// <summary>The hand-entered amounts for a month, by employee id.</summary>
        Task<Dictionary<int, HrPayrollMonthlyAmount>> GetMonthlyAmountsAsync(int year, byte month);

        /// <summary>Writes a month's amounts, replacing whatever was there.</summary>
        Task<int> SaveMonthlyAmountsAsync(int year, byte month, List<HrPayrollMonthlyAmount> rows, int? userId);

        // Staff advances, entered by date (HrStaffAdvance).
        Task<Dictionary<int, decimal>> GetAdvanceTotalsAsync(int year, byte month);

        /// <summary>Each leave type's Paid flag and Deduct Days.</summary>
        Task<Dictionary<int, (bool IsPaid, decimal DeductDays)>> GetLeaveTypeCostsAsync();
        Task<List<StaffAdvanceDto>> GetAdvancesAsync(DateTime from, DateTime to, int? employeeId, int? campusId);
        Task<HrStaffAdvance?> FindAdvanceAsync(int id);
        void AddAdvance(HrStaffAdvance advance);
        void RemoveAdvance(HrStaffAdvance advance);
        Task SaveChangesAsync();
        Task<bool> IsPayMonthClosedAsync(int employeeId, DateTime date);

        // Extra days (Scripts/ExtraDays.sql)
        Task<int> GetExtraDaysThresholdAsync();
        Task<Dictionary<int, decimal>> GetExtraDaysRulesAsync();
        Task<Dictionary<int, decimal>> GetExtraDaysAsync(int year, byte month);
        Task SaveExtraDaysAsync(int year, byte month, int minWorkingDays,
            Dictionary<int, decimal> rules, Dictionary<int, decimal> perEmployee,
            IEnumerable<int> employeesOnScreen, int? userId);
    }
}

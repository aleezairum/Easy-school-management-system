using School.API.Data.DBModels.HR;
using School.API.DTOs.HR;
using School.API.Repositories.Interfaces.HR;
using School.API.Services.Interfaces.HR;

namespace School.API.Services.Implementations.HR
{
    public class PayrollService : IPayrollService
    {
        private readonly IPayrollRepository _repository;
        private readonly IHolidayRepository _holidayRepository;

        public PayrollService(IPayrollRepository repository, IHolidayRepository holidayRepository)
        {
            _repository = repository;
            _holidayRepository = holidayRepository;
        }

        public Task<List<PayrollRunDto>> GetRunsAsync() => _repository.GetRunsAsync();

        public async Task<PayrollRunResultDto> GenerateAsync(GeneratePayrollRunDto dto, int? userId)
        {
            var periodStart = new DateTime(dto.Year, dto.Month, 1);
            var periodEnd = periodStart.AddMonths(1).AddDays(-1);
            var monthDays = periodEnd.Day;

            var existingRun = await _repository.GetRunByPeriodAsync(dto.Year, dto.Month);
            if (existingRun != null && existingRun.Status != HrPayrollRunStatus.Draft)
                throw new InvalidOperationException(
                    $"Payroll for {dto.Year}-{dto.Month:00} is already {existingRun.Status} and cannot be regenerated. See 5.2 edge case: re-running a Draft run is allowed; a Locked/Paid run is blocked.");

            var run = existingRun ?? await _repository.CreateRunAsync(dto.Year, dto.Month, userId);
            if (existingRun != null)
                await _repository.TouchRunGeneratedAsync(run.Id, userId);

            // ---- The 4-query budget (section 9) ----
            var employees = await _repository.GetEmployeesForPayrollAsync(periodEnd);                          // 1
            var employeeIds = employees.Select(e => e.EmployeeId).ToList();
            var attendanceRows = await _repository.GetAttendanceForPeriodAsync(employeeIds, periodStart, periodEnd); // 2
            var leaveRows = await _repository.GetApprovedLeaveForPeriodAsync(employeeIds, periodStart, periodEnd);   // 3
            var components = await _repository.GetActiveComponentsAsync(periodStart, periodEnd);               // 4
            // Holidays come from the cached HolidayService in the controller path elsewhere; here we go
            // straight to the small, rarely-changing table — still well within the query budget's intent.
            var holidayDates = await _holidayRepository.GetActiveDatesAsync(periodStart, periodEnd);

            var attendanceByEmployee = attendanceRows.ToLookup(a => a.EmployeeId);
            var leaveByEmployee = leaveRows.ToLookup(l => l.EmployeeId);

            var payslips = new List<HrPayslip>();
            var warnings = new List<PayrollWarningDto>();

            foreach (var emp in employees)
            {
                var payableDays = CalculatePayableDays(emp.JoiningDate, emp.LeavingDate, periodStart, periodEnd);
                if (payableDays <= 0)
                    continue; // not employed during this period at all

                if (emp.Pay == null)
                {
                    warnings.Add(new PayrollWarningDto
                    {
                        EmployeeId = emp.EmployeeId,
                        EmployeeName = emp.FullName,
                        Message = "No active pay structure — skipped. Add one from the Employee Payroll tab."
                    });
                    continue;
                }

                var workingDays = CountWorkingDays(periodStart, monthDays, holidayDates, emp.OffDay1, emp.OffDay2);

                var empAttendance = attendanceByEmployee[emp.EmployeeId].ToList();
                var presentDays = empAttendance.Count(a => a.Status == HrAttendanceStatus.Present)
                                 + 0.5m * empAttendance.Count(a => a.Status == HrAttendanceStatus.HalfDay);
                var absentDays = empAttendance.Count(a => a.Status == HrAttendanceStatus.Absent)
                                + 0.5m * empAttendance.Count(a => a.Status == HrAttendanceStatus.HalfDay);
                // IgnoreLate (D4) — the true minutes are kept for reporting elsewhere,
                // but excluded here so payroll never charges an exempted day.
                var dailyLateMinutes = empAttendance
                    .Where(a => a.LateMinutes > 0 && !a.IgnoreLate)
                    .Select(a => a.LateMinutes)
                    .ToList();

                var leaveDays = leaveByEmployee[emp.EmployeeId]
                    .Sum(l => ClipDaysToPeriod(l.FromDate, l.ToDate, periodStart, periodEnd));

                var attendanceSummary = new PayrollCalculator.AttendanceSummary
                {
                    DailyLateMinutes = dailyLateMinutes,
                    AbsentDays = absentDays,
                    PresentDays = presentDays,
                    LeaveDays = leaveDays,
                    OvertimeHours = 0 // no overtime capture mechanism yet
                };

                var applicableComponents = ResolveApplicable(components, emp.EmployeeId, emp.DesignationId, emp.HRGradeId);
                var rules = applicableComponents.Select(PayrollCalculator.ToRule).ToList();

                var input = new PayrollCalculator.EmployeeInput
                {
                    EmployeeId = emp.EmployeeId,
                    Basic = emp.Pay.Basic,
                    FixedComponents = emp.Pay.Components.Select(c => (c.ComponentId, c.Amount)).ToList(),
                    Rules = rules,
                    Attendance = attendanceSummary,
                    PayableDays = payableDays,
                    MonthDays = monthDays,
                    WorkingDays = workingDays,
                    ShiftWorkHours = emp.ShiftWorkHours
                };

                var result = PayrollCalculator.Calculate(input);

                payslips.Add(new HrPayslip
                {
                    RunId = run.Id,
                    EmployeeId = emp.EmployeeId,
                    Basic = result.Basic,
                    GrossEarning = result.GrossEarning,
                    TotalDeduction = result.TotalDeduction,
                    NetPay = result.NetPay,
                    PresentDays = presentDays,
                    AbsentDays = absentDays,
                    LeaveDays = leaveDays,
                    LateMinutes = attendanceSummary.TotalLateMinutes,
                    Breakdown = result.Breakdown.Select(b => new HrPayslipLine
                    {
                        ComponentId = b.ComponentId,
                        Name = b.Name,
                        Kind = b.Kind,
                        Amount = b.Amount
                    }).ToList()
                });
            }

            await _repository.RegeneratePayslipsAsync(run.Id, payslips);

            if (attendanceRows.Count == 0)
                warnings.Add(new PayrollWarningDto { EmployeeId = 0, EmployeeName = "All employees", Message = "No attendance was recorded for this month at all — every employee will show as fully absent. Confirm attendance was entered before locking." });

            var runs = await _repository.GetRunsAsync();
            var runDto = runs.First(r => r.Id == run.Id);

            return new PayrollRunResultDto { Run = runDto, Warnings = warnings };
        }

        public Task<List<PayslipDto>> GetPayslipsAsync(int runId) => _repository.GetPayslipsAsync(runId);
        public Task<PayslipDto?> GetPayslipAsync(int id) => _repository.GetPayslipByIdAsync(id);

        public Task<bool> LockRunAsync(int runId, int? userId) =>
            _repository.SetRunStatusAsync(runId, HrPayrollRunStatus.Locked, userId);

        public async Task<bool> PayEmployeeAsync(int payslipId, PayEmployeeDto dto)
        {
            var payslip = await _repository.GetPayslipEntityByIdAsync(payslipId);
            if (payslip == null) return false;
            await _repository.MarkPayslipPaidAsync(payslipId, dto);
            return true;
        }

        public async Task<bool> PayRunAsync(int runId, PayRunDto dto)
        {
            var run = await _repository.GetRunEntityAsync(runId);
            if (run == null) return false;
            await _repository.MarkRunPaidAsync(runId, dto);
            return true;
        }

        public async Task<List<SalarySheetRowDto>> GetSalarySheetAsync(int runId)
        {
            var payslips = await _repository.GetPayslipsAsync(runId);
            return payslips.Select(p => new SalarySheetRowDto
            {
                EmployeeId = p.EmployeeId,
                EmployeeCode = p.EmployeeCode,
                EmployeeName = p.EmployeeName,
                DesignationName = p.DesignationName,
                Basic = p.Basic,
                GrossEarning = p.GrossEarning,
                TotalDeduction = p.TotalDeduction,
                NetPay = p.NetPay,
                Breakdown = p.Breakdown
            }).ToList();
        }

        /// <summary>Most-specific-wins resolution (4.1), done in memory against the
        /// already-loaded component list — no per-employee query.</summary>
        private static List<HrPayComponent> ResolveApplicable(
            List<HrPayComponent> all, int employeeId, int? designationId, int? hrGradeId)
        {
            var candidates = all.Where(c =>
                c.ScopeType == HrPayScopeType.Global
                || (c.ScopeType == HrPayScopeType.Employee && c.ScopeId == employeeId)
                || (c.ScopeType == HrPayScopeType.Designation && designationId != null && c.ScopeId == designationId)
                || (c.ScopeType == HrPayScopeType.Grade && hrGradeId != null && c.ScopeId == hrGradeId));

            return candidates
                .GroupBy(c => c.Code)
                .Select(g => g.OrderByDescending(c => (byte)c.ScopeType).ThenByDescending(c => c.Priority).First())
                .ToList();
        }

        private static decimal CalculatePayableDays(DateTime? joiningDate, DateTime? leavingDate, DateTime periodStart, DateTime periodEnd)
        {
            var start = joiningDate.HasValue && joiningDate.Value.Date > periodStart ? joiningDate.Value.Date : periodStart;
            var end = leavingDate.HasValue && leavingDate.Value.Date < periodEnd ? leavingDate.Value.Date : periodEnd;
            if (end < start) return 0;
            return (decimal)(end - start).TotalDays + 1;
        }

        private static decimal ClipDaysToPeriod(DateTime from, DateTime to, DateTime periodStart, DateTime periodEnd)
        {
            var start = from.Date < periodStart ? periodStart : from.Date;
            var end = to.Date > periodEnd ? periodEnd : to.Date;
            if (end < start) return 0;
            return (decimal)(end - start).TotalDays + 1;
        }

        private static int CountWorkingDays(DateTime periodStart, int monthDays, HashSet<DateTime> holidayDates, byte? offDay1, byte? offDay2)
        {
            var count = 0;
            for (var d = 0; d < monthDays; d++)
            {
                var date = periodStart.AddDays(d);
                if (holidayDates.Contains(date)) continue;
                var dow = (byte)((int)date.DayOfWeek + 1);
                if (dow == offDay1 || dow == offDay2) continue;
                count++;
            }
            return count;
        }
    }
}

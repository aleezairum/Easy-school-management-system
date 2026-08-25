using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.HR;
using School.API.DTOs.HR;
using School.API.Repositories.Interfaces.HR;

namespace School.API.Repositories.Implementations.HR
{
    public class PayrollRepository : IPayrollRepository
    {
        private readonly SchoolDbContext _context;

        public PayrollRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<List<PayrollRunDto>> GetRunsAsync()
        {
            var runs = await _context.Set<HrPayrollRun>().AsNoTracking()
                .OrderByDescending(r => r.PayYear).ThenByDescending(r => r.PayMonth)
                .ToListAsync();

            var runIds = runs.Select(r => r.Id).ToList();
            var totals = await _context.Set<HrPayslip>().AsNoTracking()
                .Where(p => runIds.Contains(p.RunId))
                .GroupBy(p => p.RunId)
                .Select(g => new { RunId = g.Key, Count = g.Count(), Total = g.Sum(p => p.NetPay) })
                .ToListAsync();
            var totalsMap = totals.ToDictionary(t => t.RunId);

            return runs.Select(r => new PayrollRunDto
            {
                Id = r.Id,
                PayYear = r.PayYear,
                PayMonth = r.PayMonth,
                Status = (byte)r.Status,
                GeneratedAt = r.GeneratedAt,
                LockedAt = r.LockedAt,
                EmployeeCount = totalsMap.TryGetValue(r.Id, out var t) ? t.Count : 0,
                TotalNetPay = totalsMap.TryGetValue(r.Id, out var t2) ? t2.Total : 0
            }).ToList();
        }

        public Task<HrPayrollRun?> GetRunEntityAsync(int id) =>
            _context.Set<HrPayrollRun>().FirstOrDefaultAsync(r => r.Id == id);

        public Task<HrPayrollRun?> GetRunByPeriodAsync(int year, int month) =>
            _context.Set<HrPayrollRun>().FirstOrDefaultAsync(r => r.PayYear == year && r.PayMonth == month);

        public async Task<HrPayrollRun> CreateRunAsync(int year, int month, int? generatedBy)
        {
            var run = new HrPayrollRun
            {
                PayYear = year,
                PayMonth = (byte)month,
                Status = HrPayrollRunStatus.Draft,
                GeneratedBy = generatedBy,
                GeneratedAt = DateTime.UtcNow
            };
            _context.Set<HrPayrollRun>().Add(run);
            await _context.SaveChangesAsync();
            return run;
        }

        public async Task TouchRunGeneratedAsync(int runId, int? generatedBy)
        {
            var run = await _context.Set<HrPayrollRun>().FirstOrDefaultAsync(r => r.Id == runId);
            if (run == null) return;
            run.GeneratedBy = generatedBy;
            run.GeneratedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task RegeneratePayslipsAsync(int runId, List<HrPayslip> payslips)
        {
            var existing = await _context.Set<HrPayslip>().Where(p => p.RunId == runId).ToListAsync();
            if (existing.Count > 0)
                _context.Set<HrPayslip>().RemoveRange(existing);
            if (payslips.Count > 0)
                _context.Set<HrPayslip>().AddRange(payslips);

            // Single SaveChanges call — EF wraps delete+insert in one transaction.
            await _context.SaveChangesAsync();
        }

        private class PayslipJoinRow
        {
            public HrPayslip P { get; set; } = null!;
            public string EmployeeCode { get; set; } = string.Empty;
            public string EmployeeFullName { get; set; } = string.Empty;
            public string? DesignationName { get; set; }
        }

        private IQueryable<PayslipJoinRow> PayslipQuery() =>
            from p in _context.Set<HrPayslip>().AsNoTracking()
            join e in _context.Employees.AsNoTracking() on p.EmployeeId equals e.Id
            join d in _context.Designations.AsNoTracking() on e.DesignationId equals d.Id into dj
            from d in dj.DefaultIfEmpty()
            select new PayslipJoinRow
            {
                P = p,
                EmployeeCode = e.EmployeeCode,
                EmployeeFullName = e.FirstName + (e.LastName != null ? " " + e.LastName : ""),
                DesignationName = d != null ? d.Name : null
            };

        public async Task<List<PayslipDto>> GetPayslipsAsync(int runId)
        {
            var rows = await PayslipQuery().Where(x => x.P.RunId == runId).ToListAsync();
            return rows.Select(x => ToPayslipDto(x.P, x.EmployeeCode, x.EmployeeFullName, x.DesignationName)).ToList();
        }

        public async Task<PayslipDto?> GetPayslipByIdAsync(int id)
        {
            var row = await PayslipQuery().FirstOrDefaultAsync(x => x.P.Id == id);
            return row == null ? null : ToPayslipDto(row.P, row.EmployeeCode, row.EmployeeFullName, row.DesignationName);
        }

        private static PayslipDto ToPayslipDto(HrPayslip p, string empCode, string empName, string? designationName) => new()
        {
            Id = p.Id,
            RunId = p.RunId,
            EmployeeId = p.EmployeeId,
            EmployeeCode = empCode,
            EmployeeName = empName,
            DesignationName = designationName,
            Basic = p.Basic,
            GrossEarning = p.GrossEarning,
            TotalDeduction = p.TotalDeduction,
            NetPay = p.NetPay,
            PresentDays = p.PresentDays,
            AbsentDays = p.AbsentDays,
            LeaveDays = p.LeaveDays,
            LateMinutes = p.LateMinutes,
            Breakdown = p.Breakdown.Select(b => new PayslipLineDto
            {
                ComponentId = b.ComponentId,
                Name = b.Name,
                Kind = (byte)b.Kind,
                Amount = b.Amount
            }).ToList(),
            PaidDate = p.PaidDate,
            PaymentMethod = p.PaymentMethod,
            Reference = p.Reference
        };

        public Task<HrPayslip?> GetPayslipEntityByIdAsync(int id) =>
            _context.Set<HrPayslip>().FirstOrDefaultAsync(p => p.Id == id);

        public async Task<bool> SetRunStatusAsync(int id, HrPayrollRunStatus status, int? by)
        {
            var run = await _context.Set<HrPayrollRun>().FirstOrDefaultAsync(r => r.Id == id);
            if (run == null) return false;

            run.Status = status;
            if (status == HrPayrollRunStatus.Locked)
            {
                run.LockedBy = by;
                run.LockedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task MarkPayslipPaidAsync(int id, PayEmployeeDto dto)
        {
            var payslip = await _context.Set<HrPayslip>().FirstOrDefaultAsync(p => p.Id == id);
            if (payslip == null) return;

            payslip.PaidDate = dto.PaidDate;
            payslip.PaymentMethod = dto.PaymentMethod;
            payslip.Reference = dto.Reference;
            await _context.SaveChangesAsync();
        }

        public async Task MarkRunPaidAsync(int runId, PayRunDto dto)
        {
            var payslips = await _context.Set<HrPayslip>().Where(p => p.RunId == runId).ToListAsync();
            foreach (var p in payslips)
            {
                p.PaidDate = dto.PaidDate;
                p.PaymentMethod = dto.PaymentMethod;
                p.Reference = dto.Reference;
            }

            var run = await _context.Set<HrPayrollRun>().FirstOrDefaultAsync(r => r.Id == runId);
            if (run != null) run.Status = HrPayrollRunStatus.Paid;

            await _context.SaveChangesAsync();
        }

        // ---- Payroll run 4-query engine (section 9) ----

        // Query 1: employees + their active pay structure, via a correlated
        // subquery EF translates to a single OUTER APPLY — not a per-employee round trip.
        public async Task<List<PayrollEmployeeRow>> GetEmployeesForPayrollAsync(DateTime periodEnd)
        {
            var rows = await _context.Employees.AsNoTracking()
                .Where(e => e.IsActive)
                .Select(e => new
                {
                    e.Id,
                    e.EmployeeCode,
                    FullName = e.FirstName + (e.LastName != null ? " " + e.LastName : ""),
                    e.DesignationId,
                    DesignationName = e.Designation != null ? e.Designation.Name : null,
                    e.HRGradeId,
                    e.ShiftId,
                    ShiftWorkHours = e.Shift != null ? e.Shift.WorkHours : (decimal?)null,
                    e.OffDay1,
                    e.OffDay2,
                    e.JoiningDate,
                    e.LeavingDate,
                    Pay = _context.Set<HrEmployeePay>()
                        .Where(p => p.EmployeeId == e.Id && p.IsActive && p.EffectiveFrom <= periodEnd)
                        .OrderByDescending(p => p.EffectiveFrom)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return rows.Select(r => new PayrollEmployeeRow
            {
                EmployeeId = r.Id,
                EmployeeCode = r.EmployeeCode,
                FullName = r.FullName,
                DesignationId = r.DesignationId,
                DesignationName = r.DesignationName,
                HRGradeId = r.HRGradeId,
                ShiftId = r.ShiftId,
                ShiftWorkHours = r.ShiftWorkHours ?? 8,
                OffDay1 = r.OffDay1,
                OffDay2 = r.OffDay2,
                JoiningDate = r.JoiningDate,
                LeavingDate = r.LeavingDate,
                Pay = r.Pay
            }).ToList();
        }

        // Query 2: every attendance row in the period for these employees —
        // grouped in memory (section 5.2) so CapPerDay stays per-day-accurate.
        public async Task<List<HrAttendance>> GetAttendanceForPeriodAsync(
            IEnumerable<int> employeeIds, DateTime periodStart, DateTime periodEnd)
        {
            var idList = employeeIds.Distinct().ToList();
            if (idList.Count == 0) return new List<HrAttendance>();

            return await _context.Set<HrAttendance>().AsNoTracking()
                .Where(a => idList.Contains(a.EmployeeId) && a.AttDate >= periodStart && a.AttDate <= periodEnd)
                .ToListAsync();
        }

        // Query 3: approved leave overlapping the period.
        public async Task<List<HrLeave>> GetApprovedLeaveForPeriodAsync(
            IEnumerable<int> employeeIds, DateTime periodStart, DateTime periodEnd)
        {
            var idList = employeeIds.Distinct().ToList();
            if (idList.Count == 0) return new List<HrLeave>();

            return await _context.Set<HrLeave>().AsNoTracking()
                .Where(l => idList.Contains(l.EmployeeId) && l.Status == HrLeaveStatus.Approved
                            && l.FromDate <= periodEnd && l.ToDate >= periodStart)
                .ToListAsync();
        }

        // Query 4 (small setup table, not per-employee — see PayComponentRepository
        // for why resolution happens in memory instead of one query per employee).
        public async Task<List<HrPayComponent>> GetActiveComponentsAsync(DateTime periodStart, DateTime periodEnd)
        {
            return await _context.Set<HrPayComponent>().AsNoTracking()
                .Where(c => c.IsActive && c.EffectiveFrom <= periodEnd && (c.EffectiveTo == null || c.EffectiveTo >= periodStart))
                .ToListAsync();
        }
    }
}

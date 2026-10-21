using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.HR;
using School.API.DTOs.HR;
using School.API.Repositories.Interfaces.HR;
using School.API.Services.Implementations.HR;

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
                .OrderByDescending(r => r.PayYear).ThenByDescending(r => r.PayMonth).ThenBy(r => r.CampusId)
                .ToListAsync();

            var campusNames = await _context.Campuses.AsNoTracking()
                .ToDictionaryAsync(c => c.VID, c => c.VName);

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
                CampusId = r.CampusId,
                CampusName = r.CampusId.HasValue && campusNames.TryGetValue(r.CampusId.Value, out var cn) ? cn : null,
                Status = (byte)r.Status,
                GeneratedAt = r.GeneratedAt,
                LockedAt = r.LockedAt,
                EmployeeCount = totalsMap.TryGetValue(r.Id, out var t) ? t.Count : 0,
                TotalNetPay = totalsMap.TryGetValue(r.Id, out var t2) ? t2.Total : 0
            }).ToList();
        }

        public Task<HrPayrollRun?> GetRunEntityAsync(int id) =>
            _context.Set<HrPayrollRun>().FirstOrDefaultAsync(r => r.Id == id);

        public Task<List<HrPayrollRun>> GetRunsForPeriodAsync(int year, int month) =>
            _context.Set<HrPayrollRun>().Where(r => r.PayYear == year && r.PayMonth == month).ToListAsync();

        public async Task<Dictionary<int, int>> GetEmployeeRunsAsync(List<int> runIds)
        {
            if (runIds.Count == 0) return new Dictionary<int, int>();
            var rows = await _context.Set<HrPayslip>().AsNoTracking()
                .Where(p => runIds.Contains(p.RunId))
                .Select(p => new { p.EmployeeId, p.RunId })
                .ToListAsync();
            return rows.GroupBy(r => r.EmployeeId).ToDictionary(g => g.Key, g => g.First().RunId);
        }

        public async Task<bool> DeleteRunAsync(int runId)
        {
            if (!await _context.Set<HrPayrollRun>().AnyAsync(r => r.Id == runId)) return false;

            // Payslips first: they hold the foreign key (EF does not model it,
            // so it would not order the two deletes itself), and their
            // breakdown lines are JSON inside them, so nothing is left behind.
            // The retrying execution strategy only allows a transaction it runs itself.
            await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                await _context.Set<HrPayslip>().Where(p => p.RunId == runId).ExecuteDeleteAsync();
                await _context.Set<HrPayrollRun>().Where(r => r.Id == runId).ExecuteDeleteAsync();
                await tx.CommitAsync();
            });
            return true;
        }

        public async Task<HrPayrollRun> CreateRunAsync(int year, int month, int? campusId, int? generatedBy)
        {
            var run = new HrPayrollRun
            {
                PayYear = year,
                PayMonth = (byte)month,
                CampusId = campusId,
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
            public int? DesignationId { get; set; }
            public int? CampusId { get; set; }
            public string? CampusName { get; set; }
            public int PayYear { get; set; }
            public byte PayMonth { get; set; }
            public HrPayrollRunStatus RunStatus { get; set; }
        }

        private IQueryable<PayslipJoinRow> PayslipQuery() =>
            from p in _context.Set<HrPayslip>().AsNoTracking()
            join r in _context.Set<HrPayrollRun>().AsNoTracking() on p.RunId equals r.Id
            join e in _context.Employees.AsNoTracking() on p.EmployeeId equals e.Id
            join d in _context.Designations.AsNoTracking() on e.DesignationId equals d.Id into dj
            from d in dj.DefaultIfEmpty()
            join c in _context.Campuses.AsNoTracking() on e.CampusId equals c.VID into cj
            from c in cj.DefaultIfEmpty()
            select new PayslipJoinRow
            {
                P = p,
                EmployeeCode = e.EmployeeCode,
                EmployeeFullName = e.FirstName + (e.LastName != null ? " " + e.LastName : ""),
                DesignationName = d != null ? d.Name : null,
                DesignationId = e.DesignationId,
                CampusId = e.CampusId,
                CampusName = c != null ? c.VName : null,
                PayYear = r.PayYear,
                PayMonth = r.PayMonth,
                RunStatus = r.Status
            };

        public async Task<List<PayslipDto>> GetPayslipsAsync(int runId)
        {
            var rows = await PayslipQuery().Where(x => x.P.RunId == runId).ToListAsync();
            return rows.Select(ToPayslipDto).ToList();
        }

        public async Task<PayslipDto?> GetPayslipByIdAsync(int id)
        {
            var row = await PayslipQuery().FirstOrDefaultAsync(x => x.P.Id == id);
            return row == null ? null : ToPayslipDto(row);
        }

        /// <summary>
        /// Every payslip of every month that falls in the range, narrowed by
        /// the employee's campus, designation, or the employee - the Salary
        /// Slips screen. A month counts when any of it is inside the range.
        /// </summary>
        public async Task<List<PayslipDto>> GetPayslipsForPeriodAsync(
            DateTime from, DateTime to, int? campusId, int? designationId, int? employeeId)
        {
            var fromKey = from.Year * 100 + from.Month;
            var toKey = to.Year * 100 + to.Month;

            var rows = await PayslipQuery()
                .Where(x => x.PayYear * 100 + x.PayMonth >= fromKey && x.PayYear * 100 + x.PayMonth <= toKey)
                .Where(x => !campusId.HasValue || x.CampusId == campusId)
                .Where(x => !designationId.HasValue || x.DesignationId == designationId)
                .Where(x => !employeeId.HasValue || x.P.EmployeeId == employeeId)
                .ToListAsync();

            return rows
                .OrderBy(x => x.PayYear).ThenBy(x => x.PayMonth)
                .ThenBy(x => x.CampusName).ThenBy(x => x.EmployeeFullName)
                .Select(ToPayslipDto)
                .ToList();
        }

        private static PayslipDto ToPayslipDto(PayslipJoinRow x)
        {
            var dto = ToPayslipDto(x.P, x.EmployeeCode, x.EmployeeFullName, x.DesignationName);
            dto.CampusName = x.CampusName;
            dto.PayYear = x.PayYear;
            dto.PayMonth = x.PayMonth;
            dto.RunStatus = (byte)x.RunStatus;
            return dto;
        }

        private static PayslipDto ToPayslipDto(HrPayslip p, string empCode, string empName, string? designationName) => new()
        {
            Id = p.Id,
            RunId = p.RunId,
            EmployeeId = p.EmployeeId,
            EmployeeCode = empCode,
            EmployeeName = empName,
            DesignationName = designationName,
            EarlyMinutes = p.EarlyMinutes,
            OutingMinutes = p.OutingMinutes,
            PerMinuteRate = p.PerMinuteRate,
            RateWorkingDays = p.RateWorkingDays,
            RateHours = p.RateHours,
            Basic = p.Basic,
            GrossEarning = p.GrossEarning,
            TotalDeduction = p.TotalDeduction,
            NetPay = p.NetPay,
            PresentDays = p.PresentDays,
            AbsentDays = p.AbsentDays,
            LeaveDays = p.LeaveDays,
            LateMinutes = p.LateMinutes,
            TotalDays = p.TotalDays,
            WorkingDays = p.WorkingDays,
            DailyPay = p.DailyPay,
            Incentive = p.Incentive,
            ExtraDuty = p.ExtraDuty,
            AcademyDuty = p.AcademyDuty,
            Prize = p.Prize,
            Fine = p.Fine,
            SecurityReturn = p.SecurityReturn,
            PayDeduction = p.PayDeduction,
            PrizeFine = p.PrizeFine,
            SecurityDeduction = p.SecurityDeduction,
            Advance = p.Advance,
            TakeHome = p.TakeHome,
            ExtraDays = p.ExtraDays,
            ExtraDaysPay = p.ExtraDaysPay,
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

        public async Task<Dictionary<int, HrPayrollMonthlyAmount>> GetMonthlyAmountsAsync(int year, byte month)
        {
            var rows = await _context.Set<HrPayrollMonthlyAmount>()
                .AsNoTracking()
                .Where(a => a.PayYear == year && a.PayMonth == month)
                .ToListAsync();

            return rows.ToDictionary(a => a.EmployeeId);
        }

        /// <summary>
        /// Writes a month's amounts. A row of all zeroes is deleted rather than
        /// stored: the office clearing a figure means "there is none", and an
        /// empty row would only sit in the way of the unique index.
        /// </summary>
        public async Task<int> SaveMonthlyAmountsAsync(
            int year, byte month, List<HrPayrollMonthlyAmount> rows, int? userId)
        {
            var ids = rows.Select(r => r.EmployeeId).ToList();
            var existing = await _context.Set<HrPayrollMonthlyAmount>()
                .Where(a => a.PayYear == year && a.PayMonth == month && ids.Contains(a.EmployeeId))
                .ToListAsync();
            var byEmployee = existing.ToDictionary(a => a.EmployeeId);

            var touched = 0;
            foreach (var row in rows)
            {
                // Advance is not on this sheet any more (Staff Advances), so it
                // neither keeps a row alive nor is written here.
                var empty = row.Incentive == 0 && row.ExtraDuty == 0 && row.AcademyDuty == 0
                            && row.SecurityReturn == 0 && row.PayDeduction == 0
                            && row.SecurityDeduction == 0 && row.Prize == 0 && row.Fine == 0
                            && string.IsNullOrWhiteSpace(row.Remarks);

                if (byEmployee.TryGetValue(row.EmployeeId, out var current))
                {
                    if (empty)
                    {
                        _context.Remove(current);
                        touched++;
                        continue;
                    }

                    current.Incentive = row.Incentive;
                    current.ExtraDuty = row.ExtraDuty;
                    current.AcademyDuty = row.AcademyDuty;
                    current.SecurityReturn = row.SecurityReturn;
                    current.PayDeduction = row.PayDeduction;
                    current.SecurityDeduction = row.SecurityDeduction;
                    current.Prize = row.Prize;
                    current.Fine = row.Fine;
                    current.PrizeFine = row.Prize - row.Fine;
                    current.Remarks = row.Remarks;
                    current.UpdatedAt = DateTime.UtcNow;
                    current.UpdatedBy = userId;
                    touched++;
                }
                else if (!empty)
                {
                    row.PayYear = year;
                    row.PayMonth = month;
                    row.PrizeFine = row.Prize - row.Fine;
                    row.Advance = 0;
                    row.CreatedAt = DateTime.UtcNow;
                    row.CreatedBy = userId;
                    _context.Add(row);
                    touched++;
                }
            }

            await _context.SaveChangesAsync();
            return touched;
        }

        // ── Staff advances ─────────────────────────────────────────────────

        /// <summary>Each employee's advances dated in that month, added up.</summary>
        public async Task<Dictionary<int, decimal>> GetAdvanceTotalsAsync(int year, byte month)
        {
            var from = new DateTime(year, month, 1);
            var to = from.AddMonths(1);
            return await _context.Set<HrStaffAdvance>().AsNoTracking()
                .Where(a => a.AdvanceDate >= from && a.AdvanceDate < to)
                .GroupBy(a => a.EmployeeId)
                .Select(g => new { g.Key, Total = g.Sum(a => a.Amount) })
                .ToDictionaryAsync(x => x.Key, x => x.Total);
        }

        public async Task<Dictionary<int, (bool IsPaid, decimal DeductDays)>> GetLeaveTypeCostsAsync()
        {
            var types = await _context.Set<HrLeaveType>().AsNoTracking()
                .Select(t => new { t.Id, t.IsPaid, t.DeductDays }).ToListAsync();
            return types.ToDictionary(t => t.Id, t => (t.IsPaid, t.DeductDays));
        }

        public async Task<List<StaffAdvanceDto>> GetAdvancesAsync(DateTime fromDate, DateTime toDate, int? employeeId, int? campusId)
        {
            var start = fromDate.Date;
            var end = toDate.Date;
            var rows = await (
                from a in _context.Set<HrStaffAdvance>().AsNoTracking()
                join e in _context.Employees.AsNoTracking() on a.EmployeeId equals e.Id
                join d in _context.Designations.AsNoTracking() on e.DesignationId equals d.Id into dj
                from d in dj.DefaultIfEmpty()
                join c in _context.Campuses.AsNoTracking() on e.CampusId equals c.VID into cj
                from c in cj.DefaultIfEmpty()
                where a.AdvanceDate >= start && a.AdvanceDate <= end
                      && (employeeId == null || a.EmployeeId == employeeId)
                      && (campusId == null || e.CampusId == campusId)
                orderby a.AdvanceDate descending, a.Id descending
                select new StaffAdvanceDto
                {
                    Id = a.Id,
                    EmployeeId = a.EmployeeId,
                    EmployeeCode = e.EmployeeCode,
                    EmployeeName = e.FirstName + (e.LastName != null ? " " + e.LastName : ""),
                    DesignationName = d != null ? d.Name : null,
                    CampusName = c != null ? c.VName : null,
                    AdvanceDate = a.AdvanceDate,
                    Amount = a.Amount,
                    Remarks = a.Remarks
                }).ToListAsync();

            // On a finished (locked or paid) salary sheet already?
            foreach (var r in rows)
                r.Locked = await IsPayMonthClosedAsync(r.EmployeeId, r.AdvanceDate);
            return rows;
        }

        public Task<HrStaffAdvance?> FindAdvanceAsync(int id) =>
            _context.Set<HrStaffAdvance>().FirstOrDefaultAsync(a => a.Id == id);

        public void AddAdvance(HrStaffAdvance advance) => _context.Add(advance);

        public void RemoveAdvance(HrStaffAdvance advance) => _context.Remove(advance);

        public Task SaveChangesAsync() => _context.SaveChangesAsync();

        /// <summary>
        /// The employee is on a locked or paid payroll for the month of that
        /// date - the advance has been taken off a salary already finished.
        /// </summary>
        public Task<bool> IsPayMonthClosedAsync(int employeeId, DateTime date) =>
            (from p in _context.Set<HrPayslip>()
             join r in _context.Set<HrPayrollRun>() on p.RunId equals r.Id
             where p.EmployeeId == employeeId && r.PayYear == date.Year && r.PayMonth == date.Month
                   && r.Status != HrPayrollRunStatus.Draft
             select p.Id).AnyAsync();

        // ── Extra days ──────────────────────────────────────────────────────

        public async Task<int> GetExtraDaysThresholdAsync()
        {
            var setting = await _context.Set<HrExtraDaysSetting>().AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == 1);

            // The script seeds the row; if it is ever missing, the documented
            // starting point is still 20, not zero (zero would qualify everybody).
            return setting?.MinWorkingDays ?? 20;
        }

        public async Task<Dictionary<int, decimal>> GetExtraDaysRulesAsync() =>
            await _context.Set<HrExtraDaysRule>().AsNoTracking()
                .ToDictionaryAsync(r => r.DesignationId, r => r.ExtraDays);

        public async Task<Dictionary<int, decimal>> GetExtraDaysAsync(int year, byte month) =>
            await _context.Set<HrPayrollExtraDays>().AsNoTracking()
                .Where(e => e.PayYear == year && e.PayMonth == month)
                .ToDictionaryAsync(e => e.EmployeeId, e => e.ExtraDays);

        /// <summary>
        /// Saves the threshold, the per-designation figures, and the month's
        /// extra days, in one SaveChanges.
        /// </summary>
        /// <param name="employeesOnScreen">
        /// Everybody the screen was showing. Any of them with no figure in
        /// <paramref name="perEmployee"/> has their extra days for this month
        /// removed — the office cleared it. Staff filtered off the screen are
        /// not touched, so saving one campus never wipes the other.
        /// </param>
        public async Task SaveExtraDaysAsync(
            int year, byte month, int minWorkingDays,
            Dictionary<int, decimal> rules, Dictionary<int, decimal> perEmployee,
            IEnumerable<int> employeesOnScreen, int? userId)
        {
            var now = DateTime.UtcNow;

            // The threshold.
            var setting = await _context.Set<HrExtraDaysSetting>().FirstOrDefaultAsync(s => s.Id == 1);
            if (setting == null)
            {
                setting = new HrExtraDaysSetting { Id = 1 };
                _context.Add(setting);
            }
            setting.MinWorkingDays = minWorkingDays;
            setting.UpdatedAt = now;
            setting.UpdatedBy = userId;

            // The remembered figure per designation.
            var ruleIds = rules.Keys.ToList();
            var existingRules = await _context.Set<HrExtraDaysRule>()
                .Where(r => ruleIds.Contains(r.DesignationId))
                .ToDictionaryAsync(r => r.DesignationId);
            foreach (var (designationId, days) in rules)
            {
                if (!existingRules.TryGetValue(designationId, out var rule))
                {
                    rule = new HrExtraDaysRule { DesignationId = designationId };
                    _context.Add(rule);
                }
                rule.ExtraDays = days;
                rule.UpdatedAt = now;
                rule.UpdatedBy = userId;
            }

            // This month's figures, for the people on screen only.
            var screenIds = employeesOnScreen.Distinct().ToList();
            var existing = await _context.Set<HrPayrollExtraDays>()
                .Where(e => e.PayYear == year && e.PayMonth == month && screenIds.Contains(e.EmployeeId))
                .ToDictionaryAsync(e => e.EmployeeId);

            foreach (var employeeId in screenIds)
            {
                perEmployee.TryGetValue(employeeId, out var days);
                existing.TryGetValue(employeeId, out var row);

                if (days <= 0)
                {
                    if (row != null) _context.Remove(row);
                    continue;
                }

                if (row == null)
                {
                    _context.Add(new HrPayrollExtraDays
                    {
                        EmployeeId = employeeId, PayYear = year, PayMonth = month,
                        ExtraDays = days, CreatedAt = now, CreatedBy = userId
                    });
                }
                else
                {
                    row.ExtraDays = days;
                    row.UpdatedAt = now;
                    row.UpdatedBy = userId;
                }
            }

            await _context.SaveChangesAsync();
        }

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
                    e.CampusId,
                    CampusName = _context.Campuses.Where(c => c.VID == e.CampusId).Select(c => c.VName).FirstOrDefault(),
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

            // Work hours and weekly off now come from the SCHOOL TIMING that
            // applies to each designation — the employee's own off days still
            // override, which is the same rule the attendance grid applies, so
            // working-day counts here cannot drift from what the grid shows.
            //
            // The period end decides which timing range is in force, so a rate
            // change halfway through the year does not rewrite last month's run.
            var timings = await _context.Set<HrSchoolTiming>()
                .AsNoTracking()
                .Where(t => t.IsActive && t.StartDate <= periodEnd)
                .OrderByDescending(t => t.StartDate).ThenByDescending(t => t.Id)
                .Select(t => new { t.DesignationId, t.OffDay1, t.OffDay2, t.WorkHours, t.TimeIn, t.TimeOut })
                .ToListAsync();

            // 0 is the school-wide bucket: Dictionary rejects a null key even
            // when the key type is int?, and designation ids start at 1.
            var byDesignation = timings
                .GroupBy(t => t.DesignationId ?? 0)
                .ToDictionary(g => g.Key, g => g.First());

            return rows.Select(r =>
            {
                var hasOwnOffDays = r.OffDay1.HasValue || r.OffDay2.HasValue;

                // The designation's own timing, else the school-wide one.
                var timing = (r.DesignationId.HasValue && byDesignation.TryGetValue(r.DesignationId.Value, out var own))
                    ? own
                    : byDesignation.TryGetValue(0, out var all) ? all : null;

                return new PayrollEmployeeRow
                {
                    EmployeeId = r.Id,
                    EmployeeCode = r.EmployeeCode,
                    FullName = r.FullName,
                    DesignationId = r.DesignationId,
                    DesignationName = r.DesignationName,
                    HRGradeId = r.HRGradeId,
                    CampusId = r.CampusId,
                    CampusName = r.CampusName,
                    // 8 remains the last resort, exactly as it was when no shift
                    // was assigned — a payroll run must not divide by zero
                    // because nobody has entered a timing yet.
                    ShiftWorkHours = timing is null
                        ? 8
                        : AttendanceCalculator.WorkHoursFor(timing.TimeIn, timing.TimeOut, timing.WorkHours),
                    OffDay1 = hasOwnOffDays ? r.OffDay1 : timing?.OffDay1,
                    OffDay2 = hasOwnOffDays ? r.OffDay2 : timing?.OffDay2,
                    JoiningDate = r.JoiningDate,
                    LeavingDate = r.LeavingDate,
                    Pay = r.Pay
                };
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

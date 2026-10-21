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

            // One campus's bill, or every campus in one run (null).
            int? campusId = dto.CampusId is > 0 ? dto.CampusId : null;
            var period = $"{periodStart:MMMM yyyy}";

            // ---- The 4-query budget (section 9) ----
            var allEmployees = await _repository.GetEmployeesForPayrollAsync(periodEnd);                       // 1
            var campusNames = allEmployees.Where(e => e.CampusId.HasValue)
                .GroupBy(e => e.CampusId!.Value)
                .ToDictionary(g => g.Key, g => g.First().CampusName ?? $"Campus #{g.Key}");
            string CampusLabel(int? id) => id is int c ? (campusNames.TryGetValue(c, out var n) ? n : $"Campus #{c}") : "All campuses";

            // A month is paid one way or the other - the whole school in one
            // run, or one run per campus - never both, or the same people
            // would be on two salary bills.
            var monthRuns = await _repository.GetRunsForPeriodAsync(dto.Year, dto.Month);
            if (campusId == null && monthRuns.Any(r => r.CampusId != null))
                throw new InvalidOperationException(
                    $"{period} is being paid campus by campus ({string.Join(", ", monthRuns.Where(r => r.CampusId != null).Select(r => CampusLabel(r.CampusId)))}). " +
                    "Generate each campus on its own, or delete those runs first to pay all campuses in one run.");
            if (campusId != null && monthRuns.FirstOrDefault(r => r.CampusId == null) is HrPayrollRun whole)
                throw new InvalidOperationException(
                    $"{period} already has one run for all campuses ({whole.Status}). " +
                    (whole.Status == HrPayrollRunStatus.Draft
                        ? "Delete that run first to pay campus by campus."
                        : "It is no longer a draft, so this month cannot be split by campus."));

            var existingRun = monthRuns.FirstOrDefault(r => r.CampusId == campusId);
            if (existingRun != null && existingRun.Status != HrPayrollRunStatus.Draft)
                throw new InvalidOperationException(
                    $"Payroll for {period} ({CampusLabel(campusId)}) is already {existingRun.Status} and cannot be regenerated. Re-running a Draft run is allowed; a Locked/Paid run is blocked.");

            var run = existingRun ?? await _repository.CreateRunAsync(dto.Year, dto.Month, campusId, userId);
            if (existingRun != null)
                await _repository.TouchRunGeneratedAsync(run.Id, userId);

            var warnings = new List<PayrollWarningDto>();
            var employees = allEmployees;
            if (campusId != null)
            {
                employees = allEmployees.Where(e => e.CampusId == campusId).ToList();

                // Somebody already on another campus's bill this month - moved
                // between campuses after it was generated - is left there, not
                // paid twice.
                var otherRuns = monthRuns.Where(r => r.Id != run.Id).ToList();
                var alreadyPaid = await _repository.GetEmployeeRunsAsync(otherRuns.Select(r => r.Id).ToList());
                foreach (var e in employees.Where(e => alreadyPaid.ContainsKey(e.EmployeeId)).ToList())
                {
                    var other = otherRuns.First(r => r.Id == alreadyPaid[e.EmployeeId]);
                    warnings.Add(new PayrollWarningDto
                    {
                        EmployeeId = e.EmployeeId,
                        EmployeeName = e.FullName,
                        Message = $"Already on the {CampusLabel(other.CampusId)} salary bill for {period} - left there, not added here."
                    });
                    employees.Remove(e);
                }

                // Staff with no campus are on no campus's bill at all.
                var noCampus = allEmployees.Where(e => e.CampusId == null
                    && CalculatePayableDays(e.JoiningDate, e.LeavingDate, periodStart, periodEnd) > 0).ToList();
                if (noCampus.Count > 0)
                    warnings.Add(new PayrollWarningDto
                    {
                        EmployeeId = 0,
                        EmployeeName = $"{noCampus.Count} member(s) of staff",
                        Message = "have no campus, so they are on no campus's salary bill: "
                                  + string.Join(", ", noCampus.Select(e => e.FullName))
                                  + ". Set their campus on the Employee form and generate again."
                    });
            }

            var employeeIds = employees.Select(e => e.EmployeeId).ToList();
            var attendanceRows = await _repository.GetAttendanceForPeriodAsync(employeeIds, periodStart, periodEnd); // 2
            var leaveRows = await _repository.GetApprovedLeaveForPeriodAsync(employeeIds, periodStart, periodEnd);   // 3
            var components = await _repository.GetActiveComponentsAsync(periodStart, periodEnd);               // 4
            // What a day of each leave type costs (Payroll Setup -> Leave Types).
            var leaveTypes = await _repository.GetLeaveTypeCostsAsync();
            // Holidays come from the cached HolidayService in the controller path elsewhere; here we go
            // straight to the small, rarely-changing table — still well within the query budget's intent.
            var holidayDates = await _holidayRepository.GetActiveDatesAsync(periodStart, periodEnd);

            // The amounts the office typed for this month — incentive, fine,
            // advance and the rest. One query for the whole run.
            var monthlyAmounts = await _repository.GetMonthlyAmountsAsync(dto.Year, (byte)dto.Month);
            // Advances handed over during the month (Staff Advances screen).
            var advances = await _repository.GetAdvanceTotalsAsync(dto.Year, (byte)dto.Month);

            // Extra days given for this month, and the threshold they were
            // given against. Checked again below: attendance may have been
            // corrected since the Extra Days screen was saved.
            var extraDaysSaved = await _repository.GetExtraDaysAsync(dto.Year, (byte)dto.Month);
            var extraDaysThreshold = await _repository.GetExtraDaysThresholdAsync();

            var attendanceByEmployee = attendanceRows.ToLookup(a => a.EmployeeId);
            var leaveByEmployee = leaveRows.ToLookup(l => l.EmployeeId);

            var payslips = new List<HrPayslip>();

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
                // Exempt is a worked day: the employee was excused from marking
                // attendance (official duty, training), not away from the job — so
                // it counts here and never below in absentDays.
                var presentDays = empAttendance.Count(a => a.Status == HrAttendanceStatus.Present)
                                 + empAttendance.Count(a => a.Status == HrAttendanceStatus.Exempt)
                                 + 0.5m * empAttendance.Count(a => a.Status == HrAttendanceStatus.HalfDay);
                var absentDays = empAttendance.Count(a => a.Status == HrAttendanceStatus.Absent)
                                + 0.5m * empAttendance.Count(a => a.Status == HrAttendanceStatus.HalfDay);
                // IgnoreLate (D4) — the true minutes are kept for reporting elsewhere,
                // but excluded here so payroll never charges an exempted day.
                var dailyLateMinutes = empAttendance
                    .Where(a => a.LateMinutes > 0 && !a.IgnoreLate)
                    .Select(a => a.LateMinutes)
                    .ToList();

                // Every leave day of the month with its leave type: the days the
                // attendance register marks Leave, and the working days of an
                // approved leave application that the register does not show
                // otherwise (a day marked Present was worked, not taken off).
                var leaveDayTypes = empAttendance
                    .Where(a => a.Status == HrAttendanceStatus.Leave)
                    .GroupBy(a => a.AttDate.Date)
                    .ToDictionary(g => g.Key, g => g.First().LeaveTypeId);
                var markedDays = empAttendance.Select(a => a.AttDate.Date).ToHashSet();
                foreach (var l in leaveByEmployee[emp.EmployeeId])
                {
                    var from = l.FromDate.Date < periodStart ? periodStart : l.FromDate.Date;
                    var to = l.ToDate.Date > periodEnd ? periodEnd : l.ToDate.Date;
                    for (var d = from; d <= to; d = d.AddDays(1))
                        if (!markedDays.Contains(d) && IsWorkingDay(d, holidayDates, emp.OffDay1, emp.OffDay2))
                            leaveDayTypes[d] = l.LeaveTypeId;
                }
                decimal leaveDays = leaveDayTypes.Count;

                // What those days cost: each by its leave type's Deduct Days
                // (0 paid, 1 unpaid, 0.5 half); an unpaid type with none set
                // costs a full day; a day with no type costs a full day. Only
                // charged at all when a Leave Deduction rule is active.
                var chargeableLeaveDays = leaveDayTypes.Values.Sum(typeId =>
                    typeId.HasValue && leaveTypes.TryGetValue(typeId.Value, out var t)
                        ? (t.DeductDays > 0 ? t.DeductDays : t.IsPaid ? 0m : 1m)
                        : 1m);

                // Minutes out of school during the day (HrAttendanceOuting),
                // charged by the SHORTLEAVE component. Only days that actually
                // had a trip out are listed, so a per-day cap or grace applies
                // to those days and not to every day of the month.
                var dailyOutingMinutes = empAttendance
                    .Where(a => a.OutingMinutes > 0)
                    .Select(a => a.OutingMinutes)
                    .ToList();

                // Leaving before the day ends, charged by the EARLY component.
                // IgnoreLate excuses a late ARRIVAL and says nothing about the
                // afternoon, so it is not consulted here.
                var dailyEarlyMinutes = empAttendance
                    .Where(a => a.EarlyMinutes > 0)
                    .Select(a => a.EarlyMinutes)
                    .ToList();

                // The duty swap. Summed for the month rather than kept per day:
                // it is a running account between two people, not something a
                // daily grace or cap applies to.
                var shortMinutes = empAttendance.Sum(a => a.ShortMinutes);
                var extraMinutes = empAttendance.Sum(a => a.ExtraMinutes);

                var attendanceSummary = new PayrollCalculator.AttendanceSummary
                {
                    DailyLateMinutes = dailyLateMinutes,
                    DailyOutingMinutes = dailyOutingMinutes,
                    DailyEarlyMinutes = dailyEarlyMinutes,
                    TotalShortMinutes = shortMinutes,
                    TotalExtraMinutes = extraMinutes,
                    AbsentDays = absentDays,
                    PresentDays = presentDays,
                    LeaveDays = leaveDays,
                    ChargeableLeaveDays = chargeableLeaveDays,
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

                // Shown on the Payroll Review so the office can check a minute's
                // price: Gross / (working days x hours x 60).
                var minuteRate = PayrollCalculator.PriceMinutes(input).Rate;

                // The salary sheet's own columns: what the office typed for this
                // person for this month, on top of everything the rules worked
                // out. Nothing here is computed — see HrPayrollMonthlyAmount.
                monthlyAmounts.TryGetValue(emp.EmployeeId, out var extra);
                var incentive = extra?.Incentive ?? 0;
                var extraDuty = extra?.ExtraDuty ?? 0;
                var academyDuty = extra?.AcademyDuty ?? 0;
                var prize = extra?.Prize ?? 0;
                var securityReturn = extra?.SecurityReturn ?? 0;
                var payDeduction = extra?.PayDeduction ?? 0;
                var fine = extra?.Fine ?? 0;
                var securityDeduction = extra?.SecurityDeduction ?? 0;
                var advance = advances.TryGetValue(emp.EmployeeId, out var adv) ? adv : 0;

                // The sheet's Daily Pay: Basic / days in the month, to the paisa.
                var dailyPay = monthDays > 0 ? Math.Round(result.Basic / monthDays, 2) : 0;

                // Extra days, paid at that same Daily Pay so the office can
                // check Extra Days x Daily Pay on one row of the printed sheet.
                // Only if the person STILL qualifies: attendance corrected
                // after the Extra Days screen was saved must not be paid past.
                decimal extraDays = 0, extraDaysPay = 0;
                if (extraDaysSaved.TryGetValue(emp.EmployeeId, out var givenDays) && givenDays > 0)
                {
                    var check = AssessExtraDays(emp, periodStart, periodEnd, holidayDates,
                                                attendanceByEmployee, leaveByEmployee, extraDaysThreshold);
                    if (check.Eligible)
                    {
                        extraDays = givenDays;
                        extraDaysPay = Math.Round(givenDays * dailyPay, 2, MidpointRounding.AwayFromZero);
                    }
                    else
                    {
                        warnings.Add(new PayrollWarningDto
                        {
                            EmployeeId = emp.EmployeeId,
                            EmployeeName = emp.FullName,
                            Message = $"{givenDays:0.##} day(s) of Extra Daily were given but NOT paid: {check.Reason}. "
                                    + "Correct the attendance or re-save the Extra Daily screen."
                        });
                    }
                }

                // Added on after the rules, so the minute rates stay rates of
                // the salary and are never inflated by a month's incentive or
                // extra days.
                var gross = result.GrossEarning + incentive + extraDuty + academyDuty + prize
                          + securityReturn + extraDaysPay;
                var deductions = result.TotalDeduction + payDeduction + fine + securityDeduction;
                var netPay = Math.Max(0, gross - deductions);

                payslips.Add(new HrPayslip
                {
                    RunId = run.Id,
                    EmployeeId = emp.EmployeeId,
                    Basic = result.Basic,
                    GrossEarning = gross,
                    TotalDeduction = deductions,
                    NetPay = netPay,
                    PresentDays = presentDays,
                    AbsentDays = absentDays,
                    LeaveDays = leaveDays,
                    LateMinutes = attendanceSummary.TotalLateMinutes,
                    EarlyMinutes = attendanceSummary.TotalEarlyMinutes,
                    OutingMinutes = attendanceSummary.TotalOutingMinutes,
                    PerMinuteRate = Math.Round(minuteRate, 6),
                    RateWorkingDays = workingDays,
                    RateHours = Math.Round(emp.ShiftWorkHours, 2),

                    // The sheet's three day columns. Working days here means
                    // days on the staff, which is what the school's sheet
                    // shows — not the holiday-less divisor the minute rates use.
                    TotalDays = monthDays,
                    WorkingDays = payableDays,
                    DailyPay = dailyPay,
                    ExtraDays = extraDays,
                    ExtraDaysPay = extraDaysPay,

                    Incentive = incentive,
                    ExtraDuty = extraDuty,
                    AcademyDuty = academyDuty,
                    Prize = prize,
                    Fine = fine,
                    SecurityReturn = securityReturn,
                    PayDeduction = payDeduction,
                    PrizeFine = prize - fine,
                    SecurityDeduction = securityDeduction,
                    Advance = advance,
                    TakeHome = Math.Max(0, netPay - advance),
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

        public Task<List<PayslipDto>> GetPayslipsForPeriodAsync(
            DateTime from, DateTime to, int? campusId, int? designationId, int? employeeId) =>
            _repository.GetPayslipsForPeriodAsync(from, to, campusId, designationId, employeeId);

        /// <summary>
        /// How each employee's minutes are priced in each month the range
        /// touches - keyed by (employee, year, month). Built the way
        /// GenerateAsync builds a payslip: the pay structure in force at the
        /// month's end, the rules that apply to the employee, the month's
        /// working days. Staff with no pay structure are left out: there is
        /// nothing to charge them from.
        /// </summary>
        public async Task<Dictionary<(int EmployeeId, int Year, int Month), PayrollCalculator.DayMinutePricing>> GetMinutePricingAsync(
            DateTime from, DateTime to, ICollection<int> employeeIds)
        {
            var result = new Dictionary<(int, int, int), PayrollCalculator.DayMinutePricing>();
            if (employeeIds.Count == 0) return result;
            var wanted = employeeIds.ToHashSet();

            for (var monthStart = new DateTime(from.Year, from.Month, 1); monthStart <= to.Date; monthStart = monthStart.AddMonths(1))
            {
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                var monthDays = monthEnd.Day;

                var employees = (await _repository.GetEmployeesForPayrollAsync(monthEnd))
                    .Where(e => wanted.Contains(e.EmployeeId) && e.Pay != null).ToList();
                if (employees.Count == 0) continue;

                var components = await _repository.GetActiveComponentsAsync(monthStart, monthEnd);
                var holidayDates = await _holidayRepository.GetActiveDatesAsync(monthStart, monthEnd);

                foreach (var emp in employees)
                {
                    var payableDays = CalculatePayableDays(emp.JoiningDate, emp.LeavingDate, monthStart, monthEnd);
                    if (payableDays <= 0) continue;

                    var input = new PayrollCalculator.EmployeeInput
                    {
                        EmployeeId = emp.EmployeeId,
                        Basic = emp.Pay!.Basic,
                        FixedComponents = emp.Pay.Components.Select(c => (c.ComponentId, c.Amount)).ToList(),
                        Rules = ResolveApplicable(components, emp.EmployeeId, emp.DesignationId, emp.HRGradeId)
                                    .Select(PayrollCalculator.ToRule).ToList(),
                        PayableDays = payableDays,
                        MonthDays = monthDays,
                        WorkingDays = CountWorkingDays(monthStart, monthDays, holidayDates, emp.OffDay1, emp.OffDay2),
                        ShiftWorkHours = emp.ShiftWorkHours
                    };
                    result[(emp.EmployeeId, monthStart.Year, monthStart.Month)] = PayrollCalculator.PriceMinutes(input);
                }
            }

            return result;
        }

        public Task<bool> LockRunAsync(int runId, int? userId) =>
            _repository.SetRunStatusAsync(runId, HrPayrollRunStatus.Locked, userId);

        /// <summary>
        /// Removes a Draft run with its payslips - to switch a month between
        /// one run for all campuses and one per campus. A Locked or Paid run is
        /// a record of what was paid and stays.
        /// </summary>
        public async Task<bool> DeleteRunAsync(int runId)
        {
            var run = await _repository.GetRunEntityAsync(runId);
            if (run == null) return false;
            if (run.Status != HrPayrollRunStatus.Draft)
                throw new InvalidOperationException($"This run is {run.Status} and cannot be deleted. Only a Draft run can.");
            return await _repository.DeleteRunAsync(runId);
        }

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

        /// <summary>
        /// Every employee on the staff at the end of that month, with whatever
        /// amounts have been typed for them. Staff with nothing typed come back
        /// with zeroes, because the screen is a sheet to fill in, not a list of
        /// what happens to be saved already.
        /// </summary>
        public async Task<List<MonthlyAmountDto>> GetMonthlyAmountsAsync(
            int year, byte month, int? campusId = null, List<int>? designationIds = null, string? sortBy = null)
        {
            var periodEnd = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            var employees = await _repository.GetEmployeesForPayrollAsync(periodEnd);
            var saved = await _repository.GetMonthlyAmountsAsync(year, month);
            var advances = await _repository.GetAdvanceTotalsAsync(year, month);

            // Campus and designation narrow the sheet the same way they narrow
            // the attendance register, so the office works one campus at a time.
            if (campusId.HasValue && campusId.Value > 0)
                employees = employees.Where(e => e.CampusId == campusId.Value).ToList();

            if (designationIds is { Count: > 0 })
                employees = employees.Where(e => e.DesignationId.HasValue
                                                 && designationIds.Contains(e.DesignationId.Value)).ToList();

            employees = SortForSheet(employees, sortBy);

            return employees.Select(e =>
            {
                saved.TryGetValue(e.EmployeeId, out var a);
                return new MonthlyAmountDto
                {
                    EmployeeId = e.EmployeeId,
                    EmployeeCode = e.EmployeeCode,
                    EmployeeName = e.FullName,
                    DesignationName = e.DesignationName,
                    CampusName = e.CampusName,
                    Basic = e.Pay?.Basic ?? 0,
                    Incentive = a?.Incentive ?? 0,
                    ExtraDuty = a?.ExtraDuty ?? 0,
                    AcademyDuty = a?.AcademyDuty ?? 0,
                    SecurityReturn = a?.SecurityReturn ?? 0,
                    Prize = a?.Prize ?? 0,
                    PayDeduction = a?.PayDeduction ?? 0,
                    Fine = a?.Fine ?? 0,
                    SecurityDeduction = a?.SecurityDeduction ?? 0,
                    Advance = advances.TryGetValue(e.EmployeeId, out var adv) ? adv : 0,
                    Remarks = a?.Remarks
                };
            }).ToList();
        }

        // ── Extra days ──────────────────────────────────────────────────────

        private sealed class ExtraDaysCheck
        {
            public int WorkingDays, Present, Leave, Absent, Half, NotMarked;
            public bool Eligible;
            public string? Reason;
        }

        /// <summary>
        /// Does this person qualify for extra days this month? The ONE place the
        /// rule is written: the Extra Days screen and payroll generation both
        /// call this, so they cannot disagree about who is owed what.
        /// </summary>
        /// <remarks>
        /// Qualifies when BOTH hold:
        ///   1. no day off — no leave, no absence, no half day, and no working
        ///      day left unmarked. An unmarked day is not assumed to be present:
        ///      a bonus for full attendance is not paid on attendance nobody
        ///      recorded. The screen names the unmarked days so they can be put
        ///      right; and
        ///   2. MORE working days than the threshold (a setting, 20 to start).
        /// Working days are the month's days less holidays and the person's own
        /// weekly off, counted only while they were on the staff. A day the
        /// register marks Holiday or Weekly Off is taken out too: the office
        /// saying the school was shut that day outranks the calendar.
        /// </remarks>
        private static ExtraDaysCheck AssessExtraDays(
            PayrollEmployeeRow emp, DateTime periodStart, DateTime periodEnd,
            HashSet<DateTime> holidayDates,
            ILookup<int, HrAttendance> attendanceByEmployee,
            ILookup<int, HrLeave> leaveByEmployee,
            int minWorkingDays)
        {
            var c = new ExtraDaysCheck();

            var from = emp.JoiningDate.HasValue && emp.JoiningDate.Value.Date > periodStart
                ? emp.JoiningDate.Value.Date : periodStart;
            var to = emp.LeavingDate.HasValue && emp.LeavingDate.Value.Date < periodEnd
                ? emp.LeavingDate.Value.Date : periodEnd;

            var byDate = attendanceByEmployee[emp.EmployeeId]
                .GroupBy(a => a.AttDate.Date)
                .ToDictionary(g => g.Key, g => g.First());
            var leaves = leaveByEmployee[emp.EmployeeId].ToList();

            for (var d = from; d <= to; d = d.AddDays(1))
            {
                if (holidayDates.Contains(d)) continue;
                var dow = (byte)((int)d.DayOfWeek + 1);
                if (dow == emp.OffDay1 || dow == emp.OffDay2) continue;

                if (byDate.TryGetValue(d, out var a))
                {
                    switch (a.Status)
                    {
                        case HrAttendanceStatus.Present:
                        case HrAttendanceStatus.Exempt:
                        case HrAttendanceStatus.ShortLeave:
                            c.WorkingDays++; c.Present++; break;
                        case HrAttendanceStatus.Leave:
                            c.WorkingDays++; c.Leave++; break;
                        case HrAttendanceStatus.Absent:
                            c.WorkingDays++; c.Absent++; break;
                        case HrAttendanceStatus.HalfDay:
                            c.WorkingDays++; c.Half++; break;
                        case HrAttendanceStatus.Holiday:
                        case HrAttendanceStatus.WeeklyOff:
                            break;   // the school was shut: not a working day
                        default:
                            c.WorkingDays++; c.NotMarked++; break;
                    }
                }
                else if (leaves.Any(l => l.FromDate.Date <= d && l.ToDate.Date >= d))
                {
                    c.WorkingDays++; c.Leave++;
                }
                else
                {
                    c.WorkingDays++; c.NotMarked++;
                }
            }

            var why = new List<string>();
            if (emp.Pay == null) why.Add("no salary record");
            if (c.WorkingDays <= minWorkingDays)
                why.Add($"only {c.WorkingDays} working days (needs more than {minWorkingDays})");
            if (c.Leave > 0) why.Add($"{c.Leave} leave");
            if (c.Absent > 0) why.Add($"{c.Absent} absent");
            if (c.Half > 0) why.Add($"{c.Half} half day");
            if (c.NotMarked > 0) why.Add($"{c.NotMarked} day(s) not marked");

            c.Eligible = why.Count == 0;
            c.Reason = c.Eligible ? null : string.Join(", ", why);
            return c;
        }

        private static List<PayrollEmployeeRow> FilterForSheet(
            List<PayrollEmployeeRow> employees, int? campusId, List<int>? designationIds)
        {
            if (campusId.HasValue && campusId.Value > 0)
                employees = employees.Where(e => e.CampusId == campusId.Value).ToList();

            if (designationIds is { Count: > 0 })
                employees = employees.Where(e => e.DesignationId.HasValue
                                                 && designationIds.Contains(e.DesignationId.Value)).ToList();
            return employees;
        }

        public async Task<ExtraDaysSheetDto> GetExtraDaysAsync(
            int year, byte month, int? minWorkingDays,
            int? campusId = null, List<int>? designationIds = null, string? sortBy = null)
        {
            var periodStart = new DateTime(year, month, 1);
            var periodEnd = periodStart.AddMonths(1).AddDays(-1);
            var monthDays = periodEnd.Day;

            var employees = await _repository.GetEmployeesForPayrollAsync(periodEnd);
            employees = employees.Where(e => CalculatePayableDays(e.JoiningDate, e.LeavingDate, periodStart, periodEnd) > 0)
                                 .ToList();
            employees = SortForSheet(FilterForSheet(employees, campusId, designationIds), sortBy);

            var ids = employees.Select(e => e.EmployeeId).ToList();
            var attendance = (await _repository.GetAttendanceForPeriodAsync(ids, periodStart, periodEnd))
                             .ToLookup(a => a.EmployeeId);
            var leaves = (await _repository.GetApprovedLeaveForPeriodAsync(ids, periodStart, periodEnd))
                         .ToLookup(l => l.EmployeeId);
            var holidays = await _holidayRepository.GetActiveDatesAsync(periodStart, periodEnd);

            var threshold = minWorkingDays ?? await _repository.GetExtraDaysThresholdAsync();
            var rules = await _repository.GetExtraDaysRulesAsync();
            var saved = await _repository.GetExtraDaysAsync(year, month);

            var sheet = new ExtraDaysSheetDto { Year = year, Month = month, MinWorkingDays = threshold };

            foreach (var e in employees)
            {
                var c = AssessExtraDays(e, periodStart, periodEnd, holidays, attendance, leaves, threshold);
                var basic = e.Pay?.Basic ?? 0;
                var dailyPay = monthDays > 0 ? Math.Round(basic / monthDays, 2) : 0;

                var isSaved = saved.TryGetValue(e.EmployeeId, out var savedDays);
                var days = !c.Eligible ? 0
                         : isSaved ? savedDays
                         : e.DesignationId.HasValue && rules.TryGetValue(e.DesignationId.Value, out var ruleDays) ? ruleDays
                         : 0;

                sheet.Rows.Add(new ExtraDaysRowDto
                {
                    EmployeeId = e.EmployeeId,
                    EmployeeCode = e.EmployeeCode,
                    EmployeeName = e.FullName,
                    DesignationId = e.DesignationId,
                    DesignationName = e.DesignationName,
                    CampusName = e.CampusName,
                    Basic = basic,
                    TotalDays = monthDays,
                    DailyPay = dailyPay,
                    WorkingDays = c.WorkingDays,
                    PresentDays = c.Present,
                    LeaveDays = c.Leave,
                    AbsentDays = c.Absent,
                    HalfDays = c.Half,
                    NotMarkedDays = c.NotMarked,
                    Eligible = c.Eligible,
                    Reason = c.Reason,
                    ExtraDays = days,
                    ExtraDaysPay = Math.Round(days * dailyPay, 2, MidpointRounding.AwayFromZero),
                    Saved = isSaved
                });
            }

            // One line per designation that appears on the sheet, carrying the
            // figure remembered for it.
            sheet.Rules = employees
                .Where(e => e.DesignationId.HasValue)
                .GroupBy(e => e.DesignationId!.Value)
                .Select(g => new ExtraDaysRuleDto
                {
                    DesignationId = g.Key,
                    DesignationName = g.First().DesignationName ?? "-",
                    ExtraDays = rules.TryGetValue(g.Key, out var r) ? r : 0
                })
                .OrderBy(r => r.DesignationName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return sheet;
        }

        public async Task<SaveExtraDaysResultDto> SaveExtraDaysAsync(SaveExtraDaysDto dto, int? userId)
        {
            if (dto.MinWorkingDays < 0 || dto.MinWorkingDays > 31)
                throw new InvalidOperationException("The working-days threshold must be between 0 and 31.");

            // Re-check every row against the attendance as it is NOW. The screen
            // may have been open while the register was corrected; nobody is
            // saved as owed extra days they no longer qualify for.
            var sheet = await GetExtraDaysAsync(dto.Year, dto.Month, dto.MinWorkingDays);
            var byId = sheet.Rows.ToDictionary(r => r.EmployeeId);

            var result = new SaveExtraDaysResultDto();
            var perEmployee = new Dictionary<int, decimal>();

            foreach (var row in dto.Rows)
            {
                if (!byId.TryGetValue(row.EmployeeId, out var current)) continue;
                var days = Math.Max(0, row.ExtraDays);

                if (days > 0 && !current.Eligible)
                {
                    result.Refused.Add($"{current.EmployeeName} ({current.Reason})");
                    continue;
                }

                if (days > 0) { perEmployee[row.EmployeeId] = days; result.Saved++; }
                else result.Cleared++;
            }

            var rules = dto.Rules
                .Where(r => r.DesignationId > 0)
                .GroupBy(r => r.DesignationId)
                .ToDictionary(g => g.Key, g => Math.Max(0, g.Last().ExtraDays));

            await _repository.SaveExtraDaysAsync(
                dto.Year, dto.Month, dto.MinWorkingDays, rules, perEmployee,
                dto.Rows.Select(r => r.EmployeeId), userId);

            return result;
        }

        /// <summary>
        /// The same order the attendance register uses, through the same
        /// code-splitting helper — two screens listing the same staff must not
        /// list them differently.
        /// </summary>
        private static List<PayrollEmployeeRow> SortForSheet(List<PayrollEmployeeRow> rows, string? sortBy)
        {
            static (string Text, long Number) Key(string? code)
                => Repositories.Implementations.HR.AttendanceRepository.CodeKey(code);

            return (sortBy ?? "code").Trim().ToLowerInvariant() switch
            {
                "name" => rows.OrderBy(r => r.FullName, StringComparer.OrdinalIgnoreCase)
                              .ThenBy(r => Key(r.EmployeeCode).Text, StringComparer.OrdinalIgnoreCase)
                              .ToList(),

                // Staff with no designation go last, not first where an empty
                // name would put them.
                "designation" => rows.OrderBy(r => string.IsNullOrWhiteSpace(r.DesignationName) ? 1 : 0)
                                     .ThenBy(r => r.DesignationName, StringComparer.OrdinalIgnoreCase)
                                     .ThenBy(r => Key(r.EmployeeCode).Text, StringComparer.OrdinalIgnoreCase)
                                     .ThenBy(r => Key(r.EmployeeCode).Number)
                                     .ToList(),

                _ => rows.OrderBy(r => Key(r.EmployeeCode).Text, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(r => Key(r.EmployeeCode).Number)
                         .ThenBy(r => r.FullName, StringComparer.OrdinalIgnoreCase)
                         .ToList()
            };
        }

        // ── Staff advances ─────────────────────────────────────────────────

        public Task<List<StaffAdvanceDto>> GetAdvancesAsync(DateTime from, DateTime to, int? employeeId, int? campusId) =>
            _repository.GetAdvancesAsync(from, to, employeeId, campusId);

        /// <summary>
        /// Adds or corrects an advance. Refused for a month whose payroll is
        /// locked or paid: that salary has been finished with the advances it
        /// had, and a change would not reach it.
        /// </summary>
        public async Task<int> SaveAdvanceAsync(int? id, SaveStaffAdvanceDto dto, int? userId)
        {
            if (dto.Amount <= 0) throw new InvalidOperationException("Enter the amount of the advance.");
            if (dto.AdvanceDate == default) throw new InvalidOperationException("Enter the date the advance was given.");
            var date = dto.AdvanceDate.Date;

            var monthEnd = new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
            var staff = await _repository.GetEmployeesForPayrollAsync(monthEnd);
            if (!staff.Any(e => e.EmployeeId == dto.EmployeeId))
                throw new InvalidOperationException("Choose a member of staff who was on the staff that month.");

            if (await _repository.IsPayMonthClosedAsync(dto.EmployeeId, date))
                throw new InvalidOperationException($"The payroll for {date:MMMM yyyy} is already locked for this person - an advance cannot be added to it.");

            HrStaffAdvance advance;
            if (id.HasValue)
            {
                advance = await _repository.FindAdvanceAsync(id.Value)
                          ?? throw new KeyNotFoundException();
                if (await _repository.IsPayMonthClosedAsync(advance.EmployeeId, advance.AdvanceDate))
                    throw new InvalidOperationException($"This advance is on the locked payroll for {advance.AdvanceDate:MMMM yyyy} and cannot be changed.");
                advance.UpdatedAt = DateTime.UtcNow;
                advance.UpdatedBy = userId;
            }
            else
            {
                advance = new HrStaffAdvance { CreatedAt = DateTime.UtcNow, CreatedBy = userId };
                _repository.AddAdvance(advance);
            }

            advance.EmployeeId = dto.EmployeeId;
            advance.AdvanceDate = date;
            advance.Amount = Math.Round(dto.Amount, 2);
            advance.Remarks = string.IsNullOrWhiteSpace(dto.Remarks) ? null : dto.Remarks.Trim()[..Math.Min(255, dto.Remarks.Trim().Length)];
            await _repository.SaveChangesAsync();
            return advance.Id;
        }

        public async Task DeleteAdvanceAsync(int id)
        {
            var advance = await _repository.FindAdvanceAsync(id) ?? throw new KeyNotFoundException();
            if (await _repository.IsPayMonthClosedAsync(advance.EmployeeId, advance.AdvanceDate))
                throw new InvalidOperationException($"This advance is on the locked payroll for {advance.AdvanceDate:MMMM yyyy} and cannot be deleted.");
            _repository.RemoveAdvance(advance);
            await _repository.SaveChangesAsync();
        }

        public Task<int> SaveMonthlyAmountsAsync(SaveMonthlyAmountsDto dto, int? userId)
            => _repository.SaveMonthlyAmountsAsync(
                dto.Year, dto.Month,
                dto.Rows.Select(r => new HrPayrollMonthlyAmount
                {
                    EmployeeId = r.EmployeeId,
                    Incentive = r.Incentive,
                    ExtraDuty = Math.Max(0, r.ExtraDuty),
                    AcademyDuty = Math.Max(0, r.AcademyDuty),
                    SecurityReturn = r.SecurityReturn,
                    PayDeduction = r.PayDeduction,
                    Prize = Math.Max(0, r.Prize),
                    Fine = Math.Max(0, r.Fine),
                    SecurityDeduction = r.SecurityDeduction,
                    Remarks = string.IsNullOrWhiteSpace(r.Remarks) ? null : r.Remarks.Trim()
                }).ToList(),
                userId);

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

        private static bool IsWorkingDay(DateTime date, HashSet<DateTime> holidayDates, byte? offDay1, byte? offDay2)
        {
            if (holidayDates.Contains(date)) return false;
            var dow = (byte)((int)date.DayOfWeek + 1);
            return dow != offDay1 && dow != offDay2;
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

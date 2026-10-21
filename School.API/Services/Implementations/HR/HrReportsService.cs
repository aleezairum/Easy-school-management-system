using School.API.Data.DBModels.HR;
using School.API.DTOs.HR;
using School.API.Repositories.Interfaces.HR;
using School.API.Services.Interfaces.HR;

namespace School.API.Services.Implementations.HR
{
    public class HrReportsService : IHrReportsService
    {
        private readonly IHrReportsRepository _reports;
        private readonly IPayrollRepository _payrollRepository;
        private readonly IPayrollService _payroll;
        private readonly IHolidayRepository _holidays;

        public HrReportsService(IHrReportsRepository reports, IPayrollRepository payrollRepository,
                                IPayrollService payroll, IHolidayRepository holidays)
        {
            _reports = reports;
            _payrollRepository = payrollRepository;
            _payroll = payroll;
            _holidays = holidays;
        }

        public static readonly Dictionary<byte, string> StatusLabels = new()
        {
            [(byte)HrAttendanceStatus.Present] = "Present",
            [(byte)HrAttendanceStatus.Absent] = "Absent",
            [(byte)HrAttendanceStatus.Leave] = "Leave",
            [(byte)HrAttendanceStatus.HalfDay] = "Half Day",
            [(byte)HrAttendanceStatus.Holiday] = "Holiday",
            [(byte)HrAttendanceStatus.WeeklyOff] = "Weekly Off",
            [(byte)HrAttendanceStatus.ShortLeave] = "Short Leave",
            [(byte)HrAttendanceStatus.Exempt] = "Exempt"
        };

        public async Task<List<AttendanceReportRowDto>> GetAttendanceRowsAsync(
            string type, DateTime from, DateTime to, HrReportFilter filter)
        {
            var rows = await PricedRowsAsync(from, to, filter);

            rows = (type ?? "daily").ToLowerInvariant() switch
            {
                "late" => rows.Where(r => r.LateMinutes > 0).ToList(),
                "early" => rows.Where(r => r.EarlyMinutes > 0).ToList(),
                "outing" => rows.Where(r => r.OutingMinutes > 0).ToList(),
                "absent" => rows.Where(r => r.Status == (byte)HrAttendanceStatus.Absent).ToList(),
                _ => rows
            };

            rows.ForEach(RoundRow);
            return rows;
        }

        public async Task<List<AttendanceSummaryRowDto>> GetAttendanceSummaryAsync(DateTime from, DateTime to, HrReportFilter filter)
        {
            var rows = await PricedRowsAsync(from, to, filter);
            return rows
                .GroupBy(r => r.EmployeeId)
                .Select(Summarise)
                .OrderBy(s => s.CampusName).ThenBy(s => s.EmployeeName)
                .ToList();
        }

        public async Task<List<AttendanceCardDto>> GetAttendanceCardsAsync(DateTime from, DateTime to, HrReportFilter filter)
        {
            from = from.Date; to = to.Date;

            // Active staff on the staff at some point in the range, with their
            // weekly off days already resolved from Staff Timings.
            var staff = (await _payrollRepository.GetEmployeesForPayrollAsync(to))
                .Where(e => (!filter.CampusId.HasValue || e.CampusId == filter.CampusId)
                         && (!filter.DesignationId.HasValue || e.DesignationId == filter.DesignationId)
                         && (!filter.EmployeeId.HasValue || e.EmployeeId == filter.EmployeeId)
                         && (e.JoiningDate == null || e.JoiningDate.Value.Date <= to)
                         && (e.LeavingDate == null || e.LeavingDate.Value.Date >= from))
                .OrderBy(e => e.CampusName).ThenBy(e => e.FullName)
                .ToList();
            if (staff.Count == 0) return new List<AttendanceCardDto>();

            var rows = await PricedRowsAsync(from, to, filter);
            var byDay = rows.ToDictionary(r => (r.EmployeeId, r.AttDate.Date));
            var holidayNames = await _holidays.GetActiveNamesAsync(from, to);
            var timings = (await _reports.GetTimingDaysAsync(from, to))
                .GroupBy(t => (t.DesignationId ?? 0, t.TDate.Date))
                .ToDictionary(g => g.Key, g => g.First());
            var leave = (await _payrollRepository.GetApprovedLeaveForPeriodAsync(
                    staff.Select(s => s.EmployeeId).ToList(), from, to))
                .ToLookup(l => l.EmployeeId);
            var today = DateTime.Today;

            var cards = new List<AttendanceCardDto>();
            foreach (var emp in staff)
            {
                var card = new AttendanceCardDto
                {
                    EmployeeId = emp.EmployeeId,
                    EmployeeCode = emp.EmployeeCode,
                    EmployeeName = emp.FullName,
                    DesignationName = emp.DesignationName,
                    CampusName = emp.CampusName,
                    JoiningDate = emp.JoiningDate,
                    From = from,
                    To = to
                };

                for (var date = from; date <= to; date = date.AddDays(1))
                {
                    var day = new AttendanceCardDayDto { Date = date };

                    // The designation's hours for the day, else the school's.
                    if ((emp.DesignationId.HasValue && timings.TryGetValue((emp.DesignationId.Value, date), out var t))
                        || timings.TryGetValue((0, date), out t))
                    {
                        day.DutyIn = t.TimeIn;
                        day.DutyOut = t.TimeOut;
                    }

                    holidayNames.TryGetValue(date, out var holidayName);
                    day.HolidayName = holidayName;

                    if (byDay.TryGetValue((emp.EmployeeId, date), out var r))
                    {
                        day.Status = r.Status;
                        day.Label = StatusLabels.TryGetValue(r.Status, out var l) ? l : "-";
                        day.TimeIn = r.TimeIn;
                        day.TimeOut = r.TimeOut;
                        day.LateMinutes = r.LateMinutes;
                        day.IgnoreLate = r.IgnoreLate;
                        day.EarlyMinutes = r.EarlyMinutes;
                        day.OutingMinutes = r.OutingMinutes;
                        day.LateDeduction = Math.Round(r.LateDeduction, 2);
                        day.EarlyDeduction = Math.Round(r.EarlyDeduction, 2);
                        day.OutingDeduction = Math.Round(r.OutingDeduction, 2);
                        day.Remarks = r.Remarks;
                    }
                    else if ((emp.JoiningDate.HasValue && date < emp.JoiningDate.Value.Date)
                             || (emp.LeavingDate.HasValue && date > emp.LeavingDate.Value.Date))
                        day.Label = "Not on staff";
                    else if (holidayName != null)
                    {
                        day.Label = "Holiday";
                        card.HolidayDays++;
                    }
                    else if (IsOffDay(date, emp.OffDay1, emp.OffDay2))
                    {
                        day.Label = "Weekly Off";
                        card.OffDays++;
                    }
                    else if (leave[emp.EmployeeId].Any(lv => lv.FromDate.Date <= date && lv.ToDate.Date >= date))
                        day.Label = "Leave (approved, not marked)";
                    else if (date > today)
                        day.Label = "";
                    else
                    {
                        day.Label = "Not marked";
                        card.NotMarkedDays++;
                    }

                    card.Days.Add(day);
                }

                var mine = rows.Where(r => r.EmployeeId == emp.EmployeeId).ToList();
                card.Totals = mine.Count > 0 ? Summarise(mine) : new AttendanceSummaryRowDto
                {
                    EmployeeId = emp.EmployeeId, EmployeeCode = emp.EmployeeCode, EmployeeName = emp.FullName,
                    DesignationName = emp.DesignationName, CampusName = emp.CampusName
                };
                cards.Add(card);
            }

            return cards;
        }

        // ── helpers ─────────────────────────────────────────────────────

        /// <summary>The range's rows with each day's minutes priced, unrounded.</summary>
        private async Task<List<AttendanceReportRowDto>> PricedRowsAsync(DateTime from, DateTime to, HrReportFilter filter)
        {
            var rows = await _reports.GetAttendanceRowsAsync(from, to, filter);
            if (rows.Count == 0) return rows;

            var pricing = await _payroll.GetMinutePricingAsync(from, to, rows.Select(r => r.EmployeeId).Distinct().ToList());
            foreach (var r in rows)
            {
                if (!pricing.TryGetValue((r.EmployeeId, r.AttDate.Year, r.AttDate.Month), out var p))
                {
                    r.NoSalaryRecord = true;
                    continue;
                }

                // An excused late arrival is shown and never charged - payroll
                // leaves it out the same way.
                r.LateDeduction = r.IgnoreLate ? 0 : p.Late(r.LateMinutes);
                r.EarlyDeduction = p.Early(r.EarlyMinutes);
                r.OutingDeduction = p.Outing(r.OutingMinutes);
            }
            return rows;
        }

        /// <summary>
        /// One employee's rows added up. Late minutes and count leave out
        /// excused days, as the payslip does, so the two agree.
        /// </summary>
        private static AttendanceSummaryRowDto Summarise(IEnumerable<AttendanceReportRowDto> group)
        {
            var g = group.ToList();
            var first = g[0];
            var charged = g.Where(r => r.LateMinutes > 0 && !r.IgnoreLate).ToList();
            return new AttendanceSummaryRowDto
            {
                EmployeeId = first.EmployeeId,
                EmployeeCode = first.EmployeeCode,
                EmployeeName = first.EmployeeName,
                DesignationName = first.DesignationName,
                CampusName = first.CampusName,
                PresentDays = g.Count(r => r.Status == (byte)HrAttendanceStatus.Present),
                AbsentDays = g.Count(r => r.Status == (byte)HrAttendanceStatus.Absent),
                LeaveDays = g.Count(r => r.Status == (byte)HrAttendanceStatus.Leave),
                HalfDays = g.Count(r => r.Status == (byte)HrAttendanceStatus.HalfDay),
                ShortLeaveDays = g.Count(r => r.Status == (byte)HrAttendanceStatus.ShortLeave),
                ExemptDays = g.Count(r => r.Status == (byte)HrAttendanceStatus.Exempt),
                LateCount = charged.Count,
                LateMinutes = charged.Sum(r => r.LateMinutes),
                LateDeduction = Math.Round(g.Sum(r => r.LateDeduction), 2),
                EarlyCount = g.Count(r => r.EarlyMinutes > 0),
                EarlyMinutes = g.Sum(r => r.EarlyMinutes),
                EarlyDeduction = Math.Round(g.Sum(r => r.EarlyDeduction), 2),
                OutingCount = g.Count(r => r.OutingMinutes > 0),
                OutingMinutes = g.Sum(r => r.OutingMinutes),
                OutingDeduction = Math.Round(g.Sum(r => r.OutingDeduction), 2),
                NoSalaryRecord = g.Any(r => r.NoSalaryRecord)
            };
        }

        private static void RoundRow(AttendanceReportRowDto r)
        {
            r.LateDeduction = Math.Round(r.LateDeduction, 2);
            r.EarlyDeduction = Math.Round(r.EarlyDeduction, 2);
            r.OutingDeduction = Math.Round(r.OutingDeduction, 2);
        }

        // Off days are stored 1 = Sunday ... 7 = Saturday, as payroll counts them.
        private static bool IsOffDay(DateTime date, byte? off1, byte? off2)
        {
            var dow = (byte)((int)date.DayOfWeek + 1);
            return dow == off1 || dow == off2;
        }
    }
}

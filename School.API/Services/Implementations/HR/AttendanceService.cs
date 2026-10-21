using School.API.Data.DBModels.HR;
using School.API.DTOs.HR;
using School.API.Repositories.Interfaces.HR;
using School.API.Services.Interfaces.HR;

namespace School.API.Services.Implementations.HR
{
    public class AttendanceService : IAttendanceService
    {
        private readonly IAttendanceRepository _repository;
        private readonly IHolidayRepository _holidayRepository;
        private readonly ISchoolTimingRepository _schoolTimingRepository;

        public AttendanceService(
            IAttendanceRepository repository,
            IHolidayRepository holidayRepository,
            ISchoolTimingRepository schoolTimingRepository)
        {
            _repository = repository;
            _holidayRepository = holidayRepository;
            _schoolTimingRepository = schoolTimingRepository;
        }

        public async Task<AttendanceMonthGridDto> GetMonthGridAsync(int year, int month, List<int>? designationIds, int? campusId = null, string? sortBy = null)
        {
            var from = new DateTime(year, month, 1);
            var to = from.AddMonths(1).AddDays(-1);
            var daysInMonth = to.Day;

            var employees = await _repository.GetEmployeesAsync(designationIds, campusId, sortBy, from, to);
            var employeeIds = employees.Select(e => e.Id).ToList();

            var holidayNames = await _holidayRepository.GetActiveNamesAsync(from, to);
            var attendanceRows = await _repository.GetAttendanceRowsAsync(employeeIds, from, to);

            ApplyTimingOffDays(employees, await _schoolTimingRepository.GetOffDaysAsync(from, to));

            // Trips out of school for the whole month in one query, rather than
            // one per cell.
            var outings = await _repository.GetOutingsAsync(attendanceRows.Values.Select(a => a.Id));

            var grid = new AttendanceMonthGridDto { Year = year, Month = month };
            for (var d = 1; d <= daysInMonth; d++)
            {
                var date = new DateTime(year, month, d);
                grid.Days.Add(new AttendanceGridDayDto
                {
                    Day = d,
                    IsHoliday = holidayNames.ContainsKey(date),
                    HolidayName = holidayNames.TryGetValue(date, out var name) ? name : null
                });
            }

            foreach (var emp in employees)
            {
                var row = new AttendanceEmployeeRowDto
                {
                    EmployeeId = emp.Id,
                    EmployeeCode = emp.EmployeeCode,
                    FullName = emp.FullName,
                    OffDay1 = emp.EffectiveOffDay1,
                    OffDay2 = emp.EffectiveOffDay2
                };

                for (var d = 1; d <= daysInMonth; d++)
                {
                    var date = new DateTime(year, month, d);

                    // Before they joined, or after they left: not their day.
                    // Checked ahead of everything else — a day they were not
                    // employed is not a holiday or a weekly off for them, and
                    // it must not read as unmarked either.
                    if (!emp.EmployedOn(date))
                    {
                        row.Cells.Add(new AttendanceCellDto { Day = d, Status = (byte)HrAttendanceStatus.NotEmployed });
                        continue;
                    }

                    if (attendanceRows.TryGetValue((emp.Id, date), out var existing))
                    {
                        row.Cells.Add(new AttendanceCellDto
                        {
                            Day = d,
                            Status = (byte)existing.Status,
                            TimeIn = existing.TimeIn,
                            TimeOut = existing.TimeOut,
                            LateMinutes = existing.LateMinutes,
                            EarlyMinutes = existing.EarlyMinutes,
                            WorkedMinutes = existing.WorkedMinutes,
                            OutingMinutes = existing.OutingMinutes,
                            ShortMinutes = existing.ShortMinutes,
                            ExtraMinutes = existing.ExtraMinutes,
                            Outings = ToOutingDtos(outings, existing.Id),
                            LeaveTypeId = existing.LeaveTypeId,
                            IgnoreLate = existing.IgnoreLate,
                            Remarks = existing.Remarks
                        });
                    }
                    else if (holidayNames.ContainsKey(date))
                    {
                        row.Cells.Add(new AttendanceCellDto { Day = d, Status = (byte)HrAttendanceStatus.Holiday });
                    }
                    else if (IsWeeklyOff(date, emp.EffectiveOffDay1, emp.EffectiveOffDay2))
                    {
                        row.Cells.Add(new AttendanceCellDto { Day = d, Status = (byte)HrAttendanceStatus.WeeklyOff });
                    }
                    else
                    {
                        row.Cells.Add(new AttendanceCellDto { Day = d, Status = 0 }); // 0 = not yet entered
                    }
                }

                grid.Employees.Add(row);
            }

            return grid;
        }

        /// <summary>
        /// One date, every employee — the shape daily entry actually needs.
        /// </summary>
        /// <remarks>
        /// Each row carries the times its day runs on: the school timing set for
        /// this date if there is one (HR &gt; School Timings), otherwise the
        /// employee's own shift. That is the same rule <see cref="SaveBulkAsync"/>
        /// calculates lateness with, so what the screen shows and what payroll
        /// charges cannot drift apart.
        /// </remarks>
        public async Task<AttendanceDayGridDto> GetDayGridAsync(DateTime date, List<int>? designationIds, int? campusId = null, string? sortBy = null)
        {
            var day = date.Date;

            var employees = await _repository.GetEmployeesAsync(designationIds, campusId, sortBy, day, day);
            var employeeIds = employees.Select(e => e.Id).ToList();

            var holidayNames = await _holidayRepository.GetActiveNamesAsync(day, day);
            var schoolTimings = await _schoolTimingRepository.GetDaysByDateAsync(day, day);
            var attendanceRows = await _repository.GetAttendanceRowsAsync(employeeIds, day, day);

            ApplyTimingOffDays(employees, await _schoolTimingRepository.GetOffDaysAsync(day, day));

            // The day's trips out of school, so the screen can show and edit them.
            var outings = await _repository.GetOutingsAsync(attendanceRows.Values.Select(a => a.Id));

            // The header shows the school-wide timing; each row below shows the
            // one that actually applies to that employee's designation, which
            // may differ.
            var schoolWide = TimingFor(schoolTimings, day, null);

            var grid = new AttendanceDayGridDto
            {
                Date = day,
                IsHoliday = holidayNames.ContainsKey(day),
                HolidayName = holidayNames.TryGetValue(day, out var hName) ? hName : null,
                SchoolTimeIn = schoolWide?.TimeIn,
                SchoolTimeOut = schoolWide?.TimeOut
            };

            foreach (var emp in employees)
            {
                var timing = TimingFor(schoolTimings, day, emp.DesignationId);

                var row = new AttendanceDayRowDto
                {
                    EmployeeId = emp.Id,
                    EmployeeCode = emp.EmployeeCode,
                    FullName = emp.FullName,
                    EffectiveTimeIn = timing?.TimeIn,
                    EffectiveTimeOut = timing?.TimeOut,
                    IsWeeklyOff = IsWeeklyOff(day, emp.EffectiveOffDay1, emp.EffectiveOffDay2)
                };

                if (attendanceRows.TryGetValue((emp.Id, day), out var existing))
                {
                    row.Status = (byte)existing.Status;
                    row.TimeIn = existing.TimeIn;
                    row.TimeOut = existing.TimeOut;
                    row.LateMinutes = existing.LateMinutes;
                    row.EarlyMinutes = existing.EarlyMinutes;
                    row.WorkedMinutes = existing.WorkedMinutes;
                    row.OutingMinutes = existing.OutingMinutes;
                    row.ShortMinutes = existing.ShortMinutes;
                    row.ExtraMinutes = existing.ExtraMinutes;
                    row.Outings = ToOutingDtos(outings, existing.Id);
                    row.LeaveTypeId = existing.LeaveTypeId;
                    row.IgnoreLate = existing.IgnoreLate;
                    row.Remarks = existing.Remarks;
                }
                else if (grid.IsHoliday)
                {
                    row.Status = (byte)HrAttendanceStatus.Holiday;
                }
                else if (row.IsWeeklyOff)
                {
                    row.Status = (byte)HrAttendanceStatus.WeeklyOff;
                }
                // else Status stays 0 — not entered yet

                grid.Employees.Add(row);
            }

            return grid;
        }

        /// <summary>
        /// Works the saved attendance for a date range out again against the
        /// timings as they stand NOW, through exactly the same calculation a
        /// save uses.
        /// </summary>
        /// <remarks>
        /// Late, early and worked minutes are stored when a day is saved, so
        /// changing a timing afterwards left them describing hours that no
        /// longer applied. Two real cases:
        ///   - a day the school shut early (everyone out at 12:45) recorded as
        ///     everyone leaving early — until the day's timing is set to 12:45
        ///     AND the day is worked out again; and
        ///   - days graded Half Day by the old rule (a late arrival or early
        ///     leaving turned into half a day's absence), which under the
        ///     current rule are simply Present with their minutes charged.
        ///
        /// Kept as they were: short and extra minutes, trips out, Ignore Late,
        /// remarks, and every status a person chose rather than the calculator
        /// — leave, holiday, weekly off, short leave, exempt, and any day with no
        /// times at all. Only Present, Absent and Half Day days that carry times
        /// are graded afresh, because those are the ones the calculator made.
        /// A day no timing covers is left alone and counted as skipped.
        /// </remarks>
        public async Task<AttendanceRecalculateResultDto> RecalculateAsync(DateTime from, DateTime to, int? userId)
        {
            from = from.Date; to = to.Date;
            var result = new AttendanceRecalculateResultDto();

            var employees = await _repository.GetEmployeesAsync(null);
            var ids = employees.Select(e => e.Id).ToList();
            var byDesignation = employees.ToDictionary(e => e.Id, e => e.DesignationId);

            var before = await _repository.GetAttendanceRowsAsync(ids, from, to);
            var schoolTimings = await _schoolTimingRepository.GetDaysByDateAsync(from, to);

            var calculatorMade = new[]
            {
                HrAttendanceStatus.Present, HrAttendanceStatus.Absent, HrAttendanceStatus.HalfDay
            };

            var cells = new List<SaveAttendanceCellDto>();
            foreach (var row in before.Values)
            {
                result.Rows++;
                var regrade = row.TimeIn.HasValue && calculatorMade.Contains(row.Status);

                if (regrade && TimingFor(schoolTimings, row.AttDate.Date,
                        byDesignation.TryGetValue(row.EmployeeId, out var d) ? d : null) is null)
                {
                    result.Skipped++;
                    continue;
                }

                cells.Add(new SaveAttendanceCellDto
                {
                    EmployeeId = row.EmployeeId,
                    AttDate = row.AttDate.Date,
                    TimeIn = row.TimeIn,
                    TimeOut = row.TimeOut,
                    // A status somebody chose is sent back as chosen.
                    StatusOverride = regrade ? null : (byte)row.Status,
                    LeaveTypeId = row.LeaveTypeId,
                    IgnoreLate = row.IgnoreLate,
                    Remarks = row.Remarks,
                    // Null = leave alone: the duty swap and the trips out.
                    ShortMinutes = null,
                    ExtraMinutes = null,
                    Outings = null
                });
            }

            if (cells.Count > 0)
                await SaveBulkAsync(new SaveAttendanceBulkDto { Cells = cells }, userId);

            var after = await _repository.GetAttendanceRowsAsync(ids, from, to);
            foreach (var (key, was) in before)
            {
                if (!after.TryGetValue(key, out var now)) continue;
                if (now.Status != was.Status) result.StatusChanged++;
                if (now.LateMinutes != was.LateMinutes || now.EarlyMinutes != was.EarlyMinutes
                    || now.WorkedMinutes != was.WorkedMinutes)
                    result.MinutesChanged++;
            }

            result.Recalculated = cells.Count;
            return result;
        }

        public async Task SaveBulkAsync(SaveAttendanceBulkDto dto, int? userId)
        {
            if (dto.Cells.Count == 0) return;

            var from = dto.Cells.Min(c => c.AttDate.Date);
            var to = dto.Cells.Max(c => c.AttDate.Date);
            var employeeIds = dto.Cells.Select(c => c.EmployeeId).Distinct().ToList();

            var employees = (await _repository.GetEmployeesAsync(null))
                .Where(e => employeeIds.Contains(e.Id))
                .ToDictionary(e => e.Id);

            var holidayDates = await _holidayRepository.GetActiveDatesAsync(from, to);
            var approvedLeaves = await _repository.GetApprovedLeaveDatesAsync(employeeIds, from, to);

            // On a date the school has set timings for, those timings are when the
            // day starts and ends — for everyone. See Scripts/HrSchoolTiming.sql.
            var schoolTimings = await _schoolTimingRepository.GetDaysByDateAsync(from, to);

            // A recorded time has to be judged against something, and the school
            // timing is now the only thing that can judge it. Name the employees
            // with no timing covering the date rather than failing mid-loop on a
            // row the caller cannot identify.
            var unanswerable = dto.Cells
                .Where(c => c.TimeIn.HasValue
                            && !c.StatusOverride.HasValue
                            && employees.TryGetValue(c.EmployeeId, out var e)
                            && TimingFor(schoolTimings, c.AttDate.Date, e.DesignationId) is null)
                .Select(c => employees[c.EmployeeId].FullName)
                .Distinct()
                .ToList();

            if (unanswerable.Count > 0)
                throw new InvalidOperationException(
                    "No school timing covers that date for these employees, so their times "
                    + "cannot be judged as late or on time: "
                    + string.Join(", ", unanswerable.Take(5))
                    + (unanswerable.Count > 5 ? $" and {unanswerable.Count - 5} more" : "")
                    + ". Add a School Timing range covering the date — for their designation, "
                    + "or the school-wide one that applies to everybody.");

            var rows = new List<HrAttendance>();
            foreach (var cell in dto.Cells)
            {
                if (!employees.TryGetValue(cell.EmployeeId, out var emp)) continue;

                var date = cell.AttDate.Date;

                // Nothing is recorded for a day somebody was not on the staff.
                // Enforced here, not only on the screen: payroll counts these
                // rows, and a day before joining or after leaving would be paid.
                if (!emp.EmployedOn(date)) continue;
                var leaveTypeId = cell.LeaveTypeId
                    ?? (approvedLeaves.TryGetValue((cell.EmployeeId, date), out var lt) ? lt : (int?)null);

                HrAttendanceStatus status;
                int late = 0, early = 0, worked = 0;

                if (cell.StatusOverride.HasValue)
                {
                    // Explicit override wins outright — see SaveAttendanceCellDto.StatusOverride.
                    status = (HrAttendanceStatus)cell.StatusOverride.Value;
                }
                else
                {
                    // The timing entered for this employee's designation, else the
                    // school-wide one. Everything about the day — when it runs and
                    // how lateness is judged — comes from that single row.
                    var st = TimingFor(schoolTimings, date, emp.DesignationId);

                    var shiftInfo = st is null
                        ? null
                        : AttendanceCalculator.FromSchoolTiming(
                            st.TimeIn, st.TimeOut,
                            st.Timing?.GraceMinutes ?? 0,
                            st.Timing?.HalfDayMinutes,
                            st.Timing?.FullDayMinutes);

                    var result = AttendanceCalculator.Calculate(new AttendanceCalculator.Input
                    {
                        Shift = shiftInfo,
                        TimeIn = cell.TimeIn,
                        TimeOut = cell.TimeOut,
                        IsHoliday = holidayDates.Contains(date),
                        IsWeeklyOff = IsWeeklyOff(date, emp.EffectiveOffDay1, emp.EffectiveOffDay2),
                        ApprovedLeaveTypeId = leaveTypeId
                    });

                    status = result.Status;
                    late = result.LateMinutes;
                    early = result.EarlyMinutes;
                    worked = result.WorkedMinutes;
                }

                rows.Add(new HrAttendance
                {
                    EmployeeId = cell.EmployeeId,
                    AttDate = date,
                    TimeIn = cell.TimeIn,
                    TimeOut = cell.TimeOut,
                    Status = status,
                    LateMinutes = late,
                    EarlyMinutes = early,
                    WorkedMinutes = worked,
                    // The day's total is set by the repository when it writes
                    // the outings, which is the only place that knows them.
                    OutingMinutes = 0,
                    // Null means "this save is not about the duty swap", so
                    // whatever is on the row is kept by the upsert.
                    ShortMinutes = cell.ShortMinutes ?? 0,
                    ExtraMinutes = cell.ExtraMinutes ?? 0,
                    LeaveTypeId = leaveTypeId,
                    IgnoreLate = cell.IgnoreLate,
                    Source = cell.Source ?? HrAttendanceSource.Manual,
                    Remarks = cell.Remarks,
                    CreatedBy = userId
                });
            }

            // Which saves actually carried a duty swap. The month grid has no
            // boxes for it and sends null, and those rows must keep what the
            // day screen put there.
            var dutySwapSpecified = dto.Cells
                .Where(c => c.ShortMinutes.HasValue || c.ExtraMinutes.HasValue)
                .Select(c => (c.EmployeeId, c.AttDate.Date))
                .ToHashSet();

            await _repository.UpsertAsync(rows, dutySwapSpecified);

            // Outings are saved after the attendance rows exist, because each
            // one hangs off an attendance Id. Only cells that actually carry a
            // list are touched: a null list means this save was not about them.
            var withOutings = dto.Cells.Where(c => c.Outings != null).ToList();
            if (withOutings.Count > 0)
                await _repository.ReplaceOutingsAsync(withOutings, userId);
        }

        public async Task FillAsync(FillAttendanceDto dto, int? userId)
        {
            var from = dto.FromDate.Date;
            var to = dto.ToDate.Date;

            // A bulk fill carries StatusOverride, which wins outright in
            // SaveBulkAsync — so holidays and weekly offs are skipped here or a
            // "mark all present" would stamp Present right over them.
            var holidayDates = await _holidayRepository.GetActiveDatesAsync(from, to);
            var employees = (await _repository.GetEmployeesAsync(null))
                .Where(e => dto.EmployeeIds.Contains(e.Id))
                .ToList();

            var rows = new List<SaveAttendanceCellDto>();
            for (var date = from; date <= to; date = date.AddDays(1))
            {
                if (holidayDates.Contains(date)) continue;

                foreach (var emp in employees)
                {
                    if (IsWeeklyOff(date, emp.EffectiveOffDay1, emp.EffectiveOffDay2)) continue;

                    // Days before someone joined, or after they left, are skipped
                    // exactly like a holiday: "Mark all Present" for the month
                    // must not mark a month they were not here for.
                    if (!emp.EmployedOn(date)) continue;

                    rows.Add(new SaveAttendanceCellDto
                    {
                        EmployeeId = emp.Id,
                        AttDate = date,
                        StatusOverride = dto.Status
                    });
                }
            }

            await SaveBulkAsync(new SaveAttendanceBulkDto { Cells = rows }, userId);
        }

        /// <summary>
        /// The school timing for a date and a designation: the designation's own
        /// if it has one, otherwise the school-wide row, otherwise nothing.
        /// </summary>
        /// <remarks>
        /// This one lookup is the whole of the designation rule. A school where
        /// everyone keeps the same hours enters a single school-wide range and
        /// never sees it; a school where the Principal starts earlier enters one
        /// extra range, and only the Principal picks it up.
        /// </remarks>
        private static HrSchoolTimingDay? TimingFor(
            IReadOnlyDictionary<(DateTime Date, int? DesignationId), HrSchoolTimingDay> timings,
            DateTime date, int? designationId)
        {
            if (designationId.HasValue && timings.TryGetValue((date.Date, designationId), out var own))
                return own;

            return timings.TryGetValue((date.Date, (int?)null), out var schoolWide) ? schoolWide : null;
        }

        /// <summary>
        /// Copies each employee's weekly off days down from the timing that
        /// applies to them, so <c>EffectiveOffDay*</c> has something to fall back
        /// to when the employee has none of their own.
        /// </summary>
        private static void ApplyTimingOffDays(
            IEnumerable<GridEmployeeRow> employees,
            IReadOnlyDictionary<int, (byte? OffDay1, byte? OffDay2)> offDays)
        {
            foreach (var emp in employees)
            {
                // 0 is the school-wide bucket — see ISchoolTimingRepository.GetOffDaysAsync.
                if (emp.DesignationId.HasValue && offDays.TryGetValue(emp.DesignationId.Value, out var own))
                {
                    emp.TimingOffDay1 = own.OffDay1;
                    emp.TimingOffDay2 = own.OffDay2;
                }
                else if (offDays.TryGetValue(0, out var schoolWide))
                {
                    emp.TimingOffDay1 = schoolWide.OffDay1;
                    emp.TimingOffDay2 = schoolWide.OffDay2;
                }
            }
        }

        /// <summary>
        /// The trips out of school on one attendance row, in the order they
        /// happened. An empty list when the day has none.
        /// </summary>
        private static List<AttendanceOutingDto> ToOutingDtos(
            Dictionary<long, List<HrAttendanceOuting>> outings, long attendanceId)
        {
            if (!outings.TryGetValue(attendanceId, out var rows)) return new List<AttendanceOutingDto>();

            return rows.Select(o => new AttendanceOutingDto
            {
                Id = o.Id,
                OutTime = o.OutTime,
                InTime = o.InTime,
                Minutes = o.Minutes,
                Reason = o.Reason
            }).ToList();
        }

        private static bool IsWeeklyOff(DateTime date, byte? offDay1, byte? offDay2)
        {
            var dow = (byte)((int)date.DayOfWeek + 1); // DayOfWeek: Sun=0..Sat=6 -> our convention 1=Sun..7=Sat
            return dow == offDay1 || dow == offDay2;
        }
    }
}

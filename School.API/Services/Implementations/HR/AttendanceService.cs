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

        public async Task<AttendanceMonthGridDto> GetMonthGridAsync(int year, int month, int? designationId)
        {
            var from = new DateTime(year, month, 1);
            var to = from.AddMonths(1).AddDays(-1);
            var daysInMonth = to.Day;

            var employees = await _repository.GetEmployeesAsync(designationId);
            var employeeIds = employees.Select(e => e.Id).ToList();

            var holidayNames = await _holidayRepository.GetActiveNamesAsync(from, to);
            var attendanceRows = await _repository.GetAttendanceRowsAsync(employeeIds, from, to);

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
                    ShiftId = emp.ShiftId,
                    OffDay1 = emp.EffectiveOffDay1,
                    OffDay2 = emp.EffectiveOffDay2
                };

                for (var d = 1; d <= daysInMonth; d++)
                {
                    var date = new DateTime(year, month, d);
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
        public async Task<AttendanceDayGridDto> GetDayGridAsync(DateTime date, int? designationId)
        {
            var day = date.Date;

            var employees = await _repository.GetEmployeesAsync(designationId);
            var employeeIds = employees.Select(e => e.Id).ToList();

            var shifts = await _repository.GetShiftsByIdsAsync(
                employees.Where(e => e.ShiftId.HasValue).Select(e => e.ShiftId!.Value));

            var holidayNames = await _holidayRepository.GetActiveNamesAsync(day, day);
            var schoolTimings = await _schoolTimingRepository.GetDaysByDateAsync(day, day);
            var attendanceRows = await _repository.GetAttendanceRowsAsync(employeeIds, day, day);

            schoolTimings.TryGetValue(day, out var schoolTiming);

            var grid = new AttendanceDayGridDto
            {
                Date = day,
                IsHoliday = holidayNames.ContainsKey(day),
                HolidayName = holidayNames.TryGetValue(day, out var hName) ? hName : null,
                SchoolTimeIn = schoolTiming?.TimeIn,
                SchoolTimeOut = schoolTiming?.TimeOut
            };

            foreach (var emp in employees)
            {
                HrShift? shift = null;
                if (emp.ShiftId.HasValue) shifts.TryGetValue(emp.ShiftId.Value, out shift);

                var row = new AttendanceDayRowDto
                {
                    EmployeeId = emp.Id,
                    EmployeeCode = emp.EmployeeCode,
                    FullName = emp.FullName,
                    ShiftId = emp.ShiftId,
                    ShiftName = shift?.Name,
                    ShiftTimeIn = shift?.TimeIn,
                    ShiftTimeOut = shift?.TimeOut,
                    EffectiveTimeIn = schoolTiming?.TimeIn ?? shift?.TimeIn,
                    EffectiveTimeOut = schoolTiming?.TimeOut ?? shift?.TimeOut,
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

        public async Task SaveBulkAsync(SaveAttendanceBulkDto dto, int? userId)
        {
            if (dto.Cells.Count == 0) return;

            var from = dto.Cells.Min(c => c.AttDate.Date);
            var to = dto.Cells.Max(c => c.AttDate.Date);
            var employeeIds = dto.Cells.Select(c => c.EmployeeId).Distinct().ToList();

            var employees = (await _repository.GetEmployeesAsync(null))
                .Where(e => employeeIds.Contains(e.Id))
                .ToDictionary(e => e.Id);

            var shiftIds = employees.Values.Where(e => e.ShiftId.HasValue).Select(e => e.ShiftId!.Value);
            var shifts = await _repository.GetShiftsByIdsAsync(shiftIds);

            var holidayDates = await _holidayRepository.GetActiveDatesAsync(from, to);
            var approvedLeaves = await _repository.GetApprovedLeaveDatesAsync(employeeIds, from, to);

            // On a date the school has set timings for, those timings are when the
            // day starts and ends — for everyone. See Scripts/HrSchoolTiming.sql.
            var schoolTimings = await _schoolTimingRepository.GetDaysByDateAsync(from, to);

            // A recorded time has to be judged against something. The school
            // timing for that date is enough on its own; only a date with neither
            // a school timing nor a shift is unanswerable. Name those employees
            // rather than failing mid-loop on a row the caller cannot identify.
            var unanswerable = dto.Cells
                .Where(c => c.TimeIn.HasValue
                            && !c.StatusOverride.HasValue
                            && !schoolTimings.ContainsKey(c.AttDate.Date)
                            && employees.TryGetValue(c.EmployeeId, out var e)
                            && !e.ShiftId.HasValue)
                .Select(c => employees[c.EmployeeId].FullName)
                .Distinct()
                .ToList();

            if (unanswerable.Count > 0)
                throw new InvalidOperationException(
                    "There is no school timing for that date and these employees have no shift either, "
                    + "so their times cannot be judged as late or on time: "
                    + string.Join(", ", unanswerable.Take(5))
                    + (unanswerable.Count > 5 ? $" and {unanswerable.Count - 5} more" : "")
                    + ". Add a School Timing for the date, or set a shift on each Employee's Payroll tab.");

            var rows = new List<HrAttendance>();
            foreach (var cell in dto.Cells)
            {
                if (!employees.TryGetValue(cell.EmployeeId, out var emp)) continue;

                var date = cell.AttDate.Date;
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
                    var hasSchoolTiming = schoolTimings.TryGetValue(date, out var st);

                    AttendanceCalculator.ShiftInfo? shiftInfo = null;
                    if (emp.ShiftId.HasValue && shifts.TryGetValue(emp.ShiftId.Value, out var s))
                    {
                        // The school timing (if this date has one) says WHEN the day
                        // runs; the shift still says HOW lateness is judged — grace,
                        // half-day and full-day thresholds stay the employee's own.
                        shiftInfo = new AttendanceCalculator.ShiftInfo
                        {
                            TimeIn = hasSchoolTiming ? st!.TimeIn : s.TimeIn,
                            TimeOut = hasSchoolTiming ? st!.TimeOut : s.TimeOut,
                            GraceMinutes = s.GraceMinutes,
                            HalfDayMinutes = s.HalfDayMinutes,
                            FullDayMinutes = s.FullDayMinutes
                        };
                    }
                    else if (hasSchoolTiming)
                    {
                        // No shift of their own: the school timing stands alone, so a
                        // school running on one bell needs no per-employee shifts.
                        shiftInfo = AttendanceCalculator.FromSchoolTiming(st!.TimeIn, st!.TimeOut);
                    }

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
                    ShiftId = emp.ShiftId,
                    TimeIn = cell.TimeIn,
                    TimeOut = cell.TimeOut,
                    Status = status,
                    LateMinutes = late,
                    EarlyMinutes = early,
                    WorkedMinutes = worked,
                    LeaveTypeId = leaveTypeId,
                    IgnoreLate = cell.IgnoreLate,
                    Source = HrAttendanceSource.Manual,
                    Remarks = cell.Remarks,
                    CreatedBy = userId
                });
            }

            await _repository.UpsertAsync(rows);
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

        private static bool IsWeeklyOff(DateTime date, byte? offDay1, byte? offDay2)
        {
            var dow = (byte)((int)date.DayOfWeek + 1); // DayOfWeek: Sun=0..Sat=6 -> our convention 1=Sun..7=Sat
            return dow == offDay1 || dow == offDay2;
        }
    }
}

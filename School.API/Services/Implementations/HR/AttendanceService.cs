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

        public AttendanceService(IAttendanceRepository repository, IHolidayRepository holidayRepository)
        {
            _repository = repository;
            _holidayRepository = holidayRepository;
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
                    OffDay1 = emp.OffDay1,
                    OffDay2 = emp.OffDay2
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
                    else if (IsWeeklyOff(date, emp.OffDay1, emp.OffDay2))
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
                    AttendanceCalculator.ShiftInfo? shiftInfo = null;
                    if (emp.ShiftId.HasValue && shifts.TryGetValue(emp.ShiftId.Value, out var s))
                    {
                        shiftInfo = new AttendanceCalculator.ShiftInfo
                        {
                            TimeIn = s.TimeIn,
                            TimeOut = s.TimeOut,
                            GraceMinutes = s.GraceMinutes,
                            HalfDayMinutes = s.HalfDayMinutes,
                            FullDayMinutes = s.FullDayMinutes
                        };
                    }

                    var result = AttendanceCalculator.Calculate(new AttendanceCalculator.Input
                    {
                        Shift = shiftInfo,
                        TimeIn = cell.TimeIn,
                        TimeOut = cell.TimeOut,
                        IsHoliday = holidayDates.Contains(date),
                        IsWeeklyOff = IsWeeklyOff(date, emp.OffDay1, emp.OffDay2),
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
            var rows = new List<SaveAttendanceCellDto>();
            for (var date = dto.FromDate.Date; date <= dto.ToDate.Date; date = date.AddDays(1))
            {
                foreach (var employeeId in dto.EmployeeIds)
                {
                    rows.Add(new SaveAttendanceCellDto
                    {
                        EmployeeId = employeeId,
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

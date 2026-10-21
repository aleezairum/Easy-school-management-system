using School.API.Data.DBModels.HR;
using School.API.DTOs.HR;
using School.API.DTOs.Student;
using School.API.Repositories.Interfaces.HR;
using School.API.Repositories.Interfaces.Student;
using School.API.Services.Interfaces.HR;

namespace School.API.Services.Implementations.HR.Hikvision
{
    /// <summary>
    /// Turns one punch from a machine into attendance. The same path for a
    /// pushed punch and a pulled one; a punch seen twice is stored once.
    /// </summary>
    /// <remarks>
    /// STAFF ("E&lt;employee id&gt;"): the day's first punch is the time in and
    /// the last is the time out, merged with whatever the office already
    /// entered - the earlier in and the later out win. A second punch within
    /// <see cref="OutGapMinutes"/> of the first is the same arrival, not a time
    /// out. The row is saved through AttendanceService.SaveBulkAsync, exactly
    /// as the Staff Attendance screen saves it, so late, early, payroll and the
    /// attendance card see no difference. A status the office chose (leave,
    /// exempt, short leave...) is kept.
    ///
    /// STUDENT ("S&lt;student id&gt;"): marked Present for the day through the
    /// same procedure the Mark Attendance screen uses - but only when the class
    /// teacher has not marked the day already. The teacher's mark always wins,
    /// and the absent WhatsApp message keeps working from the teacher's marks.
    ///
    /// Any punch that names a known person counts, whatever the verify result:
    /// attendance asks whether the person was there, and a recognised face is
    /// that, even when the machine refused to open a door.
    /// </remarks>
    public class HikvisionEventProcessor
    {
        private readonly IHikvisionRepository _repo;
        private readonly IAttendanceService _attendance;
        private readonly IStdAttendenceRepository _students;
        private readonly ILogger<HikvisionEventProcessor> _logger;
        private readonly int _outGapMinutes;

        public HikvisionEventProcessor(IHikvisionRepository repo, IAttendanceService attendance,
            IStdAttendenceRepository students, IConfiguration config, ILogger<HikvisionEventProcessor> logger)
        {
            _repo = repo;
            _attendance = attendance;
            _students = students;
            _logger = logger;
            _outGapMinutes = config.GetValue("Hikvision:OutGapMinutes", 30);
        }

        public int OutGapMinutes => _outGapMinutes;

        /// <summary>Stores and applies one punch. Returns false when it had been received before.</summary>
        public async Task<bool> ProcessAsync(int deviceId, HikvisionIsapiClient.AcsEvent ev, string receivedBy)
        {
            var (type, personId) = Identify(ev.EmployeeNo);
            var row = new HrDeviceEvent
            {
                DeviceId = deviceId,
                SerialNo = ev.SerialNo,
                EmployeeNo = Clip(ev.EmployeeNo, 32),
                PersonName = Clip(ev.Name, 100),
                PersonType = type,
                PersonId = personId,
                EventTime = ev.Time,
                Major = ev.Major,
                Minor = ev.Minor,
                VerifyMode = Clip(ev.VerifyMode, 40),
                ReceivedBy = receivedBy,
                CreatedAt = DateTime.UtcNow
            };

            if (!await _repo.TryAddEventAsync(row)) return false;

            try
            {
                (row.Accepted, row.Result) = ev.Major != 5
                    ? (false, "Not an access event - ignored.")
                    : type switch
                    {
                        DevicePersonType.Staff => await ApplyStaffAsync(personId!.Value, ev.Time),
                        DevicePersonType.Student => await ApplyStudentAsync(personId!.Value, ev.Time),
                        _ => (false, string.IsNullOrEmpty(ev.EmployeeNo)
                            ? "No person on the punch (stranger or failed match)."
                            : $"\"{ev.EmployeeNo}\" is not an EasySchool person (E<staff id> or S<student id>).")
                    };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Hikvision] Applying punch {Emp} at {Time} failed", ev.EmployeeNo, ev.Time);
                (row.Accepted, row.Result) = (false, "Error: " + Clip(ex.Message, 250));
            }

            await _repo.SaveChangesAsync();
            return true;
        }

        public static (DevicePersonType? Type, int? Id) Identify(string? employeeNo)
        {
            if (string.IsNullOrWhiteSpace(employeeNo) || employeeNo.Length < 2) return (null, null);
            if (!int.TryParse(employeeNo[1..], out var id) || id <= 0) return (null, null);
            return char.ToUpperInvariant(employeeNo[0]) switch
            {
                'E' => (DevicePersonType.Staff, id),
                'S' => (DevicePersonType.Student, id),
                _ => (null, null)
            };
        }

        public static string EmployeeNoFor(DevicePersonType type, int id) => (type == DevicePersonType.Staff ? "E" : "S") + id;

        // Statuses worked out from the times. Anything else was chosen by the
        // office and is kept when the times change.
        private static readonly HashSet<HrAttendanceStatus> TimeDriven = new()
        {
            HrAttendanceStatus.Present, HrAttendanceStatus.Absent, HrAttendanceStatus.HalfDay
        };

        private async Task<(bool, string)> ApplyStaffAsync(int employeeId, DateTime punch)
        {
            var date = punch.Date;
            var punches = await _repo.GetDayPunchesAsync(DevicePersonType.Staff, employeeId, date);
            var first = Minute(punches?.First ?? punch);
            var last = Minute(punches?.Last ?? punch);

            var existing = await _repo.GetStaffDayAsync(employeeId, date);

            var timeIn = first;
            if (existing?.TimeIn is TimeSpan ein && ein < timeIn) timeIn = ein;

            TimeSpan? timeOut = (last - first).TotalMinutes >= _outGapMinutes ? last : null;
            if (existing?.TimeOut is TimeSpan eout && (timeOut == null || eout > timeOut)) timeOut = eout;
            if (timeOut.HasValue && timeOut <= timeIn) timeOut = null;

            byte? keepStatus = existing != null && !TimeDriven.Contains(existing.Status) ? (byte)existing.Status : null;

            try
            {
                await _attendance.SaveBulkAsync(new SaveAttendanceBulkDto
                {
                    Cells =
                    {
                        new SaveAttendanceCellDto
                        {
                            EmployeeId = employeeId,
                            AttDate = date,
                            TimeIn = timeIn,
                            TimeOut = timeOut,
                            StatusOverride = keepStatus,
                            LeaveTypeId = existing?.LeaveTypeId,
                            IgnoreLate = existing?.IgnoreLate ?? false,
                            Remarks = existing?.Remarks,
                            Source = HrAttendanceSource.Device
                        }
                    }
                }, null);
            }
            catch (InvalidOperationException ex)
            {
                return (false, "Not saved: " + Clip(ex.Message, 250));
            }

            var saved = await _repo.GetStaffDayAsync(employeeId, date);
            if (saved == null)
                return (false, "Not saved: not on the staff that day, or not an active employee.");

            return (true, $"Staff: in {Hm(saved.TimeIn)}{(saved.TimeOut.HasValue ? ", out " + Hm(saved.TimeOut) : "")}"
                          + $" - {saved.Status}{(saved.LateMinutes > 0 ? $", late {saved.LateMinutes} min" : "")}"
                          + $"{(saved.EarlyMinutes > 0 ? $", early {saved.EarlyMinutes} min" : "")}"
                          + (keepStatus.HasValue ? " (status chosen by the office kept)" : ""));
        }

        private async Task<(bool, string)> ApplyStudentAsync(int studentId, DateTime punch)
        {
            var enrolment = await _repo.GetStudentEnrolmentAsync(studentId);
            if (enrolment == null) return (false, "Not saved: the student is not currently enrolled.");

            var mark = await _repo.GetStudentMarkAsync(studentId, enrolment.Value.SessionId, punch.Date);
            if (mark.HasValue)
                return (true, mark == 1
                    ? "Student: already present for the day."
                    : $"Student: the teacher already marked {(mark == 0 ? "Absent" : "Leave")} - not changed.");

            var result = await _students.SaveAsync(new StdAttendenceSaveDto
            {
                SessionID = enrolment.Value.SessionId,
                StudentID = studentId,
                SectionID = enrolment.Value.SectionId,
                VDate = punch.Date,
                Attendence = 1,
                IsLate = false,
                IsAgain = false,
                Remarks = $"Machine {punch:HH:mm}",
                ISentSMS = false
            }, 0, "machine");

            return result?.ReturnCode == 200
                ? (true, $"Student: marked Present ({punch:HH:mm}).")
                : (false, "Not saved: " + (result?.ReturnMessage ?? "the attendance procedure did not answer."));
        }

        private static TimeSpan Minute(DateTime t) => new(t.Hour, t.Minute, 0);
        private static string Hm(TimeSpan? t) => t.HasValue ? $"{(int)t.Value.TotalHours:00}:{t.Value.Minutes:00}" : "-";
        private static string? Clip(string? s, int max) => s == null ? null : s.Length <= max ? s : s[..max];
    }
}

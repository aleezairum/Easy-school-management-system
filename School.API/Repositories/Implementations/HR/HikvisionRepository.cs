using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.HR;
using School.API.DTOs.HR;
using School.API.Repositories.Interfaces.HR;

namespace School.API.Repositories.Implementations.HR
{
    public class HikvisionRepository : IHikvisionRepository
    {
        private readonly SchoolDbContext _context;

        public HikvisionRepository(SchoolDbContext context)
        {
            _context = context;
        }

        private SqlConnection Connect() => new(_context.Database.GetConnectionString());

        // ── Machines ───────────────────────────────────────────────────

        public async Task<List<AttendanceDeviceDto>> GetDevicesAsync()
        {
            var today = DateTime.Today;
            var devices = await _context.HrAttendanceDevices.AsNoTracking().OrderBy(d => d.Name).ToListAsync();
            var campuses = await _context.Campuses.AsNoTracking().ToDictionaryAsync(c => c.VID, c => c.VName);
            var events = await _context.HrDeviceEvents.AsNoTracking()
                .Where(e => e.EventTime >= today).GroupBy(e => e.DeviceId)
                .Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N);
            var people = await _context.HrDevicePersons.AsNoTracking()
                .Where(p => p.UserSyncedAt != null).GroupBy(p => p.DeviceId)
                .Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N);

            return devices.Select(d => new AttendanceDeviceDto
            {
                Id = d.Id,
                Name = d.Name,
                CampusId = d.CampusId,
                CampusName = d.CampusId.HasValue && campuses.TryGetValue(d.CampusId.Value, out var cn) ? cn : null,
                IpAddress = d.IpAddress,
                Port = d.Port,
                UseHttps = d.UseHttps,
                Username = d.Username,
                HasPassword = !string.IsNullOrEmpty(d.Password),
                IsActive = d.IsActive,
                PullEnabled = d.PullEnabled,
                DeviceModel = d.DeviceModel,
                SerialNumber = d.SerialNumber,
                Firmware = d.Firmware,
                LastSeenAt = d.LastSeenAt,
                LastPullAt = d.LastPullAt,
                LastEventTime = d.LastEventTime,
                LastError = d.LastError,
                PushPath = "/api/hikvision/event/" + d.PushKey,
                EventsToday = events.TryGetValue(d.Id, out var n) ? n : 0,
                PeopleOnDevice = people.TryGetValue(d.Id, out var p) ? p : 0
            }).ToList();
        }

        public Task<HrAttendanceDevice?> FindDeviceAsync(int id) =>
            _context.HrAttendanceDevices.FirstOrDefaultAsync(d => d.Id == id);

        public Task<HrAttendanceDevice?> FindDeviceByKeyAsync(string pushKey) =>
            _context.HrAttendanceDevices.FirstOrDefaultAsync(d => d.PushKey == pushKey && d.IsActive);

        public Task<List<HrAttendanceDevice>> GetPullableDevicesAsync() =>
            _context.HrAttendanceDevices.Where(d => d.IsActive && d.PullEnabled).ToListAsync();

        public async Task AddDeviceAsync(HrAttendanceDevice device)
        {
            _context.HrAttendanceDevices.Add(device);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteDeviceAsync(int id) =>
            await _context.HrAttendanceDevices.Where(d => d.Id == id).ExecuteDeleteAsync() > 0;

        public Task SaveChangesAsync() => _context.SaveChangesAsync();

        // ── Punches ────────────────────────────────────────────────────

        public async Task<bool> TryAddEventAsync(HrDeviceEvent e)
        {
            // A punch without the machine's serial number is matched on who and when.
            if (e.SerialNo == null &&
                await _context.HrDeviceEvents.AnyAsync(x => x.DeviceId == e.DeviceId && x.EmployeeNo == e.EmployeeNo
                                                            && x.EventTime == e.EventTime && x.Minor == e.Minor))
                return false;

            _context.HrDeviceEvents.Add(e);
            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
                // Already received the other way (push then pull, or the reverse).
                _context.Entry(e).State = EntityState.Detached;
                return false;
            }
        }

        public async Task<(DateTime First, DateTime Last)?> GetDayPunchesAsync(DevicePersonType type, int personId, DateTime date)
        {
            var from = date.Date;
            var to = from.AddDays(1);
            var times = await _context.HrDeviceEvents.AsNoTracking()
                .Where(e => e.PersonType == type && e.PersonId == personId && e.EventTime >= from && e.EventTime < to)
                .Select(e => e.EventTime).ToListAsync();
            return times.Count == 0 ? null : (times.Min(), times.Max());
        }

        public async Task<List<DeviceEventRowDto>> GetEventsAsync(int? deviceId, DateTime? date, int take)
        {
            var q = _context.HrDeviceEvents.AsNoTracking().AsQueryable();
            if (deviceId.HasValue) q = q.Where(e => e.DeviceId == deviceId);
            if (date.HasValue)
            {
                var from = date.Value.Date; var to = from.AddDays(1);
                q = q.Where(e => e.EventTime >= from && e.EventTime < to);
            }
            var names = await _context.HrAttendanceDevices.AsNoTracking().ToDictionaryAsync(d => d.Id, d => d.Name);
            var rows = await q.OrderByDescending(e => e.EventTime).ThenByDescending(e => e.Id).Take(take).ToListAsync();
            return rows.Select(e => new DeviceEventRowDto
            {
                Id = e.Id,
                DeviceId = e.DeviceId,
                DeviceName = names.TryGetValue(e.DeviceId, out var n) ? n : null,
                EventTime = e.EventTime,
                EmployeeNo = e.EmployeeNo,
                PersonName = e.PersonName,
                PersonType = (byte?)e.PersonType,
                Major = e.Major,
                Minor = e.Minor,
                VerifyMode = e.VerifyMode,
                ReceivedBy = e.ReceivedBy,
                Accepted = e.Accepted,
                Result = e.Result
            }).ToList();
        }

        // ── People ─────────────────────────────────────────────────────

        public async Task<Dictionary<(DevicePersonType, int), HrDevicePerson>> GetDevicePeopleAsync(int deviceId) =>
            (await _context.HrDevicePersons.AsNoTracking().Where(p => p.DeviceId == deviceId).ToListAsync())
                .ToDictionary(p => (p.PersonType, p.PersonId));

        public Task<List<HrDevicePerson>> GetPersonOnDevicesAsync(DevicePersonType type, int personId) =>
            _context.HrDevicePersons.Where(p => p.PersonType == type && p.PersonId == personId && p.UserSyncedAt != null).ToListAsync();

        public async Task<HrDevicePerson> GetOrAddDevicePersonAsync(int deviceId, DevicePersonType type, int personId, string employeeNo)
        {
            var p = await _context.HrDevicePersons.FirstOrDefaultAsync(x => x.DeviceId == deviceId && x.PersonType == type && x.PersonId == personId);
            if (p != null) return p;
            p = new HrDevicePerson { DeviceId = deviceId, PersonType = type, PersonId = personId, EmployeeNo = employeeNo, UpdatedAt = DateTime.Now };
            _context.HrDevicePersons.Add(p);
            await _context.SaveChangesAsync();
            return p;
        }

        public async Task RemoveDevicePersonAsync(int deviceId, DevicePersonType type, int personId) =>
            await _context.HrDevicePersons.Where(x => x.DeviceId == deviceId && x.PersonType == type && x.PersonId == personId).ExecuteDeleteAsync();

        public async Task<HashSet<(DevicePersonType, int)>> GetFacePhotoKeysAsync() =>
            (await _context.HrBiometricFaces.AsNoTracking().Select(f => new { f.PersonType, f.PersonId }).ToListAsync())
                .Select(f => (f.PersonType, f.PersonId)).ToHashSet();

        public async Task<byte[]?> GetFacePhotoAsync(DevicePersonType type, int personId) =>
            (await _context.HrBiometricFaces.AsNoTracking().FirstOrDefaultAsync(f => f.PersonType == type && f.PersonId == personId))?.ImageData;

        public async Task SaveFacePhotoAsync(DevicePersonType type, int personId, byte[] jpeg, int? userId, int? sourceDeviceId = null)
        {
            var f = await _context.HrBiometricFaces.FirstOrDefaultAsync(x => x.PersonType == type && x.PersonId == personId);
            if (f == null) _context.HrBiometricFaces.Add(new HrBiometricFace { PersonType = type, PersonId = personId, ImageData = jpeg, UpdatedBy = userId, UpdatedAt = DateTime.Now, SourceDeviceId = sourceDeviceId });
            else { f.ImageData = jpeg; f.UpdatedBy = userId; f.UpdatedAt = DateTime.Now; f.SourceDeviceId = sourceDeviceId; }
            await _context.SaveChangesAsync();
        }

        public async Task<Dictionary<(DevicePersonType, int), int>> GetFingerCountsAsync() =>
            (await _context.HrBiometricFingers.AsNoTracking()
                .GroupBy(f => new { f.PersonType, f.PersonId })
                .Select(g => new { g.Key.PersonType, g.Key.PersonId, Count = g.Count() })
                .ToListAsync())
            .ToDictionary(x => (x.PersonType, x.PersonId), x => x.Count);

        public Task<List<HrBiometricFinger>> GetFingersAsync(DevicePersonType type, int personId) =>
            _context.HrBiometricFingers.AsNoTracking()
                .Where(f => f.PersonType == type && f.PersonId == personId)
                .OrderBy(f => f.FingerNo).ToListAsync();

        // What the machine holds now replaces what was kept: a finger deleted on
        // the machine must not come back when the person is put on another one.
        public async Task ReplaceFingersAsync(DevicePersonType type, int personId, IEnumerable<HrBiometricFinger> fingers)
        {
            var old = await _context.HrBiometricFingers.Where(f => f.PersonType == type && f.PersonId == personId).ToListAsync();
            _context.HrBiometricFingers.RemoveRange(old);
            await _context.SaveChangesAsync();
            _context.HrBiometricFingers.AddRange(fingers);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> SetEmployeePhotoIfEmptyAsync(int employeeId, string dataUrl)
        {
            var e = await _context.Employees.FirstOrDefaultAsync(x => x.Id == employeeId);
            if (e == null || !string.IsNullOrWhiteSpace(e.Photo)) return false;
            e.Photo = dataUrl;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<(int Id, string Code, string Name, string? Detail)>> GetStaffAsync(int? campusId)
        {
            var rows = await (
                from e in _context.Employees.AsNoTracking()
                join d in _context.Designations.AsNoTracking() on e.DesignationId equals d.Id into dj
                from d in dj.DefaultIfEmpty()
                where e.IsActive && (!campusId.HasValue || e.CampusId == campusId)
                orderby e.EmployeeCode
                select new { e.Id, e.EmployeeCode, Name = e.FirstName + (e.LastName != null ? " " + e.LastName : ""), Desig = d != null ? d.Name : null })
                .ToListAsync();
            return rows.Select(r => (r.Id, r.EmployeeCode, r.Name.Trim(), r.Desig)).ToList();
        }

        public async Task<List<(int Id, string Code, string Name, string? Detail)>> GetStudentsAsync(int? campusId, int? classId, int? sectionId)
        {
            // Currently enrolled only (latest academic record, StatusID 1) - the
            // same test the WhatsApp bot and the fee procedures use.
            const string sql = @"
                SELECT S.VID, ISNULL(S.RollNo,''), S.Name, LTRIM(RTRIM(ISNULL(C.VName,'') + ' - ' + ISNULL(SEC.VName,'')))
                FROM SMSStudent S
                OUTER APPLY (SELECT TOP 1 A.SectionID, A.StatusID FROM SMSStudentAcademic A
                             WHERE A.StudentID = S.VID AND ISNULL(A.IsActive,1) = 1
                             ORDER BY A.SessionID DESC, A.VID DESC) A
                LEFT JOIN SMSSection SEC ON SEC.VID = A.SectionID
                LEFT JOIN SMSClass C ON C.VID = SEC.ClassID
                WHERE A.StatusID = 1
                  AND (@campus IS NULL OR S.CampusID = @campus)
                  AND (@class IS NULL OR C.VID = @class)
                  AND (@section IS NULL OR SEC.VID = @section)
                ORDER BY C.VID, SEC.VName, S.Name;";
            var list = new List<(int, string, string, string?)>();
            await using var cn = Connect();
            await cn.OpenAsync();
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@campus", (object?)campusId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@class", (object?)classId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@section", (object?)sectionId ?? DBNull.Value);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                list.Add((r.GetInt32(0), r.GetString(1), r.IsDBNull(2) ? "" : r.GetString(2), r.IsDBNull(3) ? null : r.GetString(3)));
            return list;
        }

        // ── What a punch writes to ─────────────────────────────────────

        public Task<HrAttendance?> GetStaffDayAsync(int employeeId, DateTime date) =>
            _context.HrAttendances.AsNoTracking().FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.AttDate == date.Date);

        public async Task<(int SessionId, int SectionId)?> GetStudentEnrolmentAsync(int studentId)
        {
            const string sql = @"
                SELECT TOP 1 A.SessionID, A.SectionID, A.StatusID FROM SMSStudentAcademic A
                WHERE A.StudentID = @s AND ISNULL(A.IsActive,1) = 1
                ORDER BY A.SessionID DESC, A.VID DESC;";
            await using var cn = Connect();
            await cn.OpenAsync();
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@s", studentId);
            await using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync()) return null;
            if (r.IsDBNull(2) || Convert.ToInt32(r.GetValue(2)) != 1) return null;
            return (Convert.ToInt32(r.GetValue(0)), Convert.ToInt32(r.GetValue(1)));
        }

        public async Task<int?> GetStudentMarkAsync(int studentId, int sessionId, DateTime date)
        {
            const string sql = @"SELECT TOP 1 Attendence FROM StdAttendence
                                 WHERE StudentID = @s AND SessionID = @ss AND VDate = @d AND IsActive = 1;";
            await using var cn = Connect();
            await cn.OpenAsync();
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@s", studentId);
            cmd.Parameters.AddWithValue("@ss", sessionId);
            cmd.Parameters.AddWithValue("@d", date.Date);
            var v = await cmd.ExecuteScalarAsync();
            return v == null || v == DBNull.Value ? null : Convert.ToInt32(v);
        }
    }
}

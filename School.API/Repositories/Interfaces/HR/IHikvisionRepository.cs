using School.API.Data.DBModels.HR;
using School.API.DTOs.HR;

namespace School.API.Repositories.Interfaces.HR
{
    public interface IHikvisionRepository
    {
        // Machines
        Task<List<AttendanceDeviceDto>> GetDevicesAsync();
        Task<HrAttendanceDevice?> FindDeviceAsync(int id);
        Task<HrAttendanceDevice?> FindDeviceByKeyAsync(string pushKey);
        Task<List<HrAttendanceDevice>> GetPullableDevicesAsync();
        Task AddDeviceAsync(HrAttendanceDevice device);
        Task<bool> DeleteDeviceAsync(int id);
        Task SaveChangesAsync();

        // Punches
        /// <summary>Stores a punch; false when the same punch was stored before (push and pull both bring it).</summary>
        Task<bool> TryAddEventAsync(HrDeviceEvent e);
        Task<(DateTime First, DateTime Last)?> GetDayPunchesAsync(DevicePersonType type, int personId, DateTime date);
        Task<List<DeviceEventRowDto>> GetEventsAsync(int? deviceId, DateTime? date, int take);

        // People
        Task<Dictionary<(DevicePersonType, int), HrDevicePerson>> GetDevicePeopleAsync(int deviceId);
        Task<List<HrDevicePerson>> GetPersonOnDevicesAsync(DevicePersonType type, int personId);
        Task<HrDevicePerson> GetOrAddDevicePersonAsync(int deviceId, DevicePersonType type, int personId, string employeeNo);
        Task RemoveDevicePersonAsync(int deviceId, DevicePersonType type, int personId);
        Task<HashSet<(DevicePersonType, int)>> GetFacePhotoKeysAsync();
        Task<byte[]?> GetFacePhotoAsync(DevicePersonType type, int personId);
        Task SaveFacePhotoAsync(DevicePersonType type, int personId, byte[] jpeg, int? userId, int? sourceDeviceId = null);

        // Fingerprint templates read from a machine
        Task<Dictionary<(DevicePersonType, int), int>> GetFingerCountsAsync();
        Task<List<HrBiometricFinger>> GetFingersAsync(DevicePersonType type, int personId);
        Task ReplaceFingersAsync(DevicePersonType type, int personId, IEnumerable<HrBiometricFinger> fingers);

        /// <summary>Puts the picture on the employee when they have none. True when it was put.</summary>
        Task<bool> SetEmployeePhotoIfEmptyAsync(int employeeId, string dataUrl);

        /// <summary>(id, code, name, detail) of active staff on a campus (all campuses when null).</summary>
        Task<List<(int Id, string Code, string Name, string? Detail)>> GetStaffAsync(int? campusId);

        /// <summary>(id, roll no, name, class - section) of enrolled students on a campus.</summary>
        Task<List<(int Id, string Code, string Name, string? Detail)>> GetStudentsAsync(int? campusId, int? classId, int? sectionId);

        // What a punch writes to
        Task<HrAttendance?> GetStaffDayAsync(int employeeId, DateTime date);

        /// <summary>The student's current session and section, or null when not enrolled now.</summary>
        Task<(int SessionId, int SectionId)?> GetStudentEnrolmentAsync(int studentId);

        /// <summary>The day's mark already recorded (1 present, 0 absent, 2 leave), or null.</summary>
        Task<int?> GetStudentMarkAsync(int studentId, int sessionId, DateTime date);
    }
}

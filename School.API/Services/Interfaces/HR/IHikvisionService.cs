using School.API.DTOs.HR;

namespace School.API.Services.Interfaces.HR
{
    /// <summary>Face-attendance machines (Hikvision): setup, punches, and who is on them.</summary>
    public interface IHikvisionService
    {
        Task<List<AttendanceDeviceDto>> GetDevicesAsync();

        /// <exception cref="InvalidOperationException">Missing name or address.</exception>
        Task<AttendanceDeviceDto> SaveDeviceAsync(int? id, SaveAttendanceDeviceDto dto);
        Task<bool> DeleteDeviceAsync(int id);
        Task<DeviceTestResultDto> TestDeviceAsync(int id);

        /// <summary>Reads the machine's punches since the last read (or since <paramref name="from"/>).</summary>
        Task<DevicePullResultDto> PullAsync(int id, DateTime? from = null);

        /// <summary>Every active machine with pulling on - the background job's tick.</summary>
        Task PullAllAsync();

        /// <summary>A punch the machine pushed. False when the key names no machine.</summary>
        Task<bool> ReceivePushAsync(string pushKey, string body, string? contentType);

        Task<List<DevicePersonRowDto>> GetPeopleAsync(int deviceId, byte personType, int? classId, int? sectionId);

        /// <summary>Asks the machine who is on it and records it against EasySchool's list.</summary>
        Task<DeviceSyncResultDto> RefreshFromDeviceAsync(int deviceId);
        Task<DeviceSyncResultDto> SyncPeopleAsync(int deviceId, DeviceSyncRequestDto dto);

        /// <summary>Reads faces and fingerprints from the machine into EasySchool (the given people, or everyone on it).</summary>
        Task<DeviceSyncResultDto> FetchBiometricsAsync(int deviceId, DeviceSyncRequestDto dto, int? userId);

        /// <summary>Reads faces and fingerprints from one machine and puts the people, with them, on another.</summary>
        Task<DeviceSyncResultDto> CopyToDeviceAsync(int sourceDeviceId, int targetDeviceId, DeviceSyncRequestDto dto, int? userId);
        Task<DeviceSyncResultDto> RemovePeopleAsync(int deviceId, DeviceSyncRequestDto dto);
        Task<DeviceSyncResultDto> SaveFaceAsync(SaveFacePhotoDto dto, int? userId);
        Task<byte[]?> GetFaceAsync(byte personType, int personId);
        Task<DeviceSyncResultDto> SaveCardAsync(int deviceId, SaveDeviceCardDto dto);
        Task<List<DeviceEventRowDto>> GetEventsAsync(int? deviceId, DateTime? date, int take);
    }
}

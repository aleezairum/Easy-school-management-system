using School.API.DTOs.Academic;

namespace School.API.DTOs.HR
{
    /// <summary>A machine as the screen shows it - the password is never sent back.</summary>
    public class AttendanceDeviceDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? CampusId { get; set; }
        public string? CampusName { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
        public bool UseHttps { get; set; }
        public string Username { get; set; } = string.Empty;
        public bool HasPassword { get; set; }
        public bool IsActive { get; set; }
        public bool PullEnabled { get; set; }
        public string? DeviceModel { get; set; }
        public string? SerialNumber { get; set; }
        public string? Firmware { get; set; }
        public DateTime? LastSeenAt { get; set; }
        public DateTime? LastPullAt { get; set; }
        public DateTime? LastEventTime { get; set; }
        public string? LastError { get; set; }

        /// <summary>The path to put in the machine's HTTP push setting, e.g. /api/hikvision/event/ab12...</summary>
        public string PushPath { get; set; } = string.Empty;
        public int EventsToday { get; set; }
        public int PeopleOnDevice { get; set; }
    }

    public class SaveAttendanceDeviceDto
    {
        public string Name { get; set; } = string.Empty;
        public int? CampusId { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; } = 80;
        public bool UseHttps { get; set; }
        public string Username { get; set; } = "admin";

        /// <summary>Null or empty keeps the password already saved.</summary>
        public string? Password { get; set; }
        public bool IsActive { get; set; } = true;
        public bool PullEnabled { get; set; } = true;
    }

    /// <summary>What "Test connection" found.</summary>
    public class DeviceTestResultDto
    {
        public bool Ok { get; set; }
        public string? Message { get; set; }
        public string? DeviceName { get; set; }
        public string? Model { get; set; }
        public string? SerialNumber { get; set; }
        public string? Firmware { get; set; }
        public string? DeviceTime { get; set; }
        public string ServerTime { get; set; } = string.Empty;
        public int? UsersOnDevice { get; set; }
    }

    public class DevicePullResultDto
    {
        public bool Ok { get; set; }
        public string? Message { get; set; }
        public int Read { get; set; }
        public int New { get; set; }
        public int Accepted { get; set; }
    }

    /// <summary>A member of staff or a student, as the Machine People list shows them.</summary>
    public class DevicePersonRowDto
    {
        public byte PersonType { get; set; }
        public int PersonId { get; set; }
        public string EmployeeNo { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        /// <summary>Designation for staff, class and section for a student.</summary>
        public string? Detail { get; set; }
        public bool OnDevice { get; set; }
        public bool FaceOnDevice { get; set; }
        public bool HasFacePhoto { get; set; }

        /// <summary>Fingerprints on the machine, as last read from it (null: not read yet).</summary>
        public int? FingersOnDevice { get; set; }

        /// <summary>Fingerprint templates kept in EasySchool, ready to put on any machine.</summary>
        public int FingersSaved { get; set; }
        public string? CardNo { get; set; }
        public DateTime? SyncedAt { get; set; }
        public string? LastError { get; set; }
    }

    public class DevicePersonRefDto
    {
        public byte PersonType { get; set; }
        public int PersonId { get; set; }
    }

    public class DeviceSyncRequestDto
    {
        public List<DevicePersonRefDto> People { get; set; } = new();

        /// <summary>Also send the face photo saved in EasySchool, where there is one.</summary>
        public bool IncludeFace { get; set; } = true;

        /// <summary>Also send the fingerprints kept in EasySchool, where there are any.</summary>
        public bool IncludeFingers { get; set; } = true;
    }

    public class DeviceSyncResultDto
    {
        public int Done { get; set; }
        public int Failed { get; set; }

        // Reading faces and fingerprints from a machine (and copying them to another)
        public int Faces { get; set; }
        public int Fingers { get; set; }
        public int PhotosSet { get; set; }
        public int Sent { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class SaveFacePhotoDto
    {
        public byte PersonType { get; set; }
        public int PersonId { get; set; }
        public UploadImageDto Image { get; set; } = new();

        /// <summary>Send it at once to every machine the person is on.</summary>
        public bool SendToMachines { get; set; } = true;
    }

    public class SaveDeviceCardDto
    {
        public byte PersonType { get; set; }
        public int PersonId { get; set; }

        /// <summary>Empty removes the card number from EasySchool's record.</summary>
        public string? CardNo { get; set; }
    }

    public class DeviceEventRowDto
    {
        public long Id { get; set; }
        public int DeviceId { get; set; }
        public string? DeviceName { get; set; }
        public DateTime EventTime { get; set; }
        public string? EmployeeNo { get; set; }
        public string? PersonName { get; set; }
        public byte? PersonType { get; set; }
        public int Major { get; set; }
        public int Minor { get; set; }
        public string? VerifyMode { get; set; }
        public string ReceivedBy { get; set; } = string.Empty;
        public bool Accepted { get; set; }
        public string? Result { get; set; }
    }
}

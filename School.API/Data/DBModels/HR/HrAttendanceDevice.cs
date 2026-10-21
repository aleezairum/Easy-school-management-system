using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Data.DBModels.HR
{
    /// <summary>
    /// A face-attendance machine (Hikvision, ISAPI). Its punches arrive by push
    /// to /api/hikvision/event/{PushKey}, and are also pulled every minute in
    /// case a push was missed. Scripts/HikvisionAttendance.sql creates the tables.
    /// </summary>
    [Table("HrAttendanceDevice")]
    public class HrAttendanceDevice
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public int? CampusId { get; set; }

        [Required, StringLength(100)]
        public string IpAddress { get; set; } = string.Empty;

        public int Port { get; set; } = 80;
        public bool UseHttps { get; set; }

        [StringLength(50)]
        public string Username { get; set; } = "admin";

        /// <summary>The machine's admin password. Never sent back to the browser.</summary>
        [StringLength(100)]
        public string? Password { get; set; }

        [Required, StringLength(40)]
        public string PushKey { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public bool PullEnabled { get; set; } = true;

        [StringLength(100)] public string? DeviceModel { get; set; }
        [StringLength(100)] public string? SerialNumber { get; set; }
        [StringLength(100)] public string? Firmware { get; set; }

        public DateTime? LastSeenAt { get; set; }
        public DateTime? LastPullAt { get; set; }
        public DateTime? LastEventTime { get; set; }

        [StringLength(500)] public string? LastError { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }

    public enum DevicePersonType : byte
    {
        Staff = 1,
        Student = 2
    }

    /// <summary>Somebody sent to a machine, and whether their face and card went too.</summary>
    [Table("HrDevicePerson")]
    public class HrDevicePerson
    {
        [Key]
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public DevicePersonType PersonType { get; set; }
        public int PersonId { get; set; }

        [StringLength(32)] public string EmployeeNo { get; set; } = string.Empty;
        [StringLength(32)] public string? CardNo { get; set; }

        public DateTime? UserSyncedAt { get; set; }
        public DateTime? FaceSyncedAt { get; set; }

        /// <summary>Fingerprints the machine holds for the person, as last read from it.</summary>
        public byte? FingersOnDevice { get; set; }

        [StringLength(500)] public string? LastError { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// A face photo, uploaded in EasySchool or read from a machine, kept to put
    /// on another machine or fill a replaced one.
    /// </summary>
    [Table("HrBiometricFace")]
    public class HrBiometricFace
    {
        public DevicePersonType PersonType { get; set; }
        public int PersonId { get; set; }
        public byte[] ImageData { get; set; } = Array.Empty<byte>();
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public int? UpdatedBy { get; set; }

        /// <summary>The machine it was read from; null when uploaded in EasySchool.</summary>
        public int? SourceDeviceId { get; set; }
    }

    /// <summary>
    /// One fingerprint template read from a machine. Hikvision terminals share
    /// the template format, so the same data enrolled on another machine lets
    /// the same finger in there without the person enrolling again.
    /// </summary>
    [Table("HrBiometricFinger")]
    public class HrBiometricFinger
    {
        public DevicePersonType PersonType { get; set; }
        public int PersonId { get; set; }

        /// <summary>The machine's fingerPrintID, 1-10.</summary>
        public byte FingerNo { get; set; }

        [StringLength(20)] public string FingerType { get; set; } = "normalFP";

        /// <summary>Base64, exactly as the machine gave it.</summary>
        public string TemplateData { get; set; } = string.Empty;

        public int? SourceDeviceId { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>One punch from a machine, and what was done with it.</summary>
    [Table("HrDeviceEvent")]
    public class HrDeviceEvent
    {
        [Key]
        public long Id { get; set; }
        public int DeviceId { get; set; }
        public long? SerialNo { get; set; }

        [StringLength(32)] public string? EmployeeNo { get; set; }
        [StringLength(100)] public string? PersonName { get; set; }

        public DevicePersonType? PersonType { get; set; }
        public int? PersonId { get; set; }

        public DateTime EventTime { get; set; }
        public int Major { get; set; }
        public int Minor { get; set; }

        [StringLength(40)] public string? VerifyMode { get; set; }

        /// <summary>push or pull.</summary>
        [StringLength(10)] public string ReceivedBy { get; set; } = "pull";

        public bool Accepted { get; set; }
        [StringLength(300)] public string? Result { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

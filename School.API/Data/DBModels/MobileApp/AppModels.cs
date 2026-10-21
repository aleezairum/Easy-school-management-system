using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Data.DBModels.MobileApp
{
    public enum AppAccountType : byte
    {
        Student = 1,
        Staff = 2
    }

    /// <summary>
    /// A mobile app login (Scripts/MobileApp.sql). One per student - a parent
    /// signs in with any of their children's logins - and one per member of staff.
    /// </summary>
    [Table("AppAccount")]
    public class AppAccount
    {
        [Key]
        public int Id { get; set; }
        public AppAccountType AccountType { get; set; }
        public int? StudentId { get; set; }
        public int? EmployeeId { get; set; }

        [StringLength(50)] public string LoginId { get; set; } = string.Empty;
        [StringLength(150)] public string? DisplayName { get; set; }

        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
        public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();

        /// <summary>The PIN the office handed out, kept to reprint the login slip; cleared once the user sets their own.</summary>
        [StringLength(20)] public string? InitialPassword { get; set; }
        public bool MustChangePassword { get; set; } = true;

        /// <summary>Staff: Roles.Id, which decides the app features (AppRoleFeature).</summary>
        public int? RoleId { get; set; }

        public bool IsActive { get; set; } = true;
        public int FailedLogins { get; set; }
        public DateTime? LockedUntil { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>A feature of the app a role may use (Mobile App Permissions).</summary>
    [Table("AppRoleFeature")]
    public class AppRoleFeature
    {
        public int RoleId { get; set; }
        [StringLength(50)] public string FeatureKey { get; set; } = string.Empty;
    }

    /// <summary>A phone signed in to the app, for push notifications.</summary>
    [Table("AppDevice")]
    public class AppDevice
    {
        [Key]
        public int Id { get; set; }
        public int AccountId { get; set; }
        [StringLength(10)] public string Mode { get; set; } = "parent";
        [StringLength(400)] public string Token { get; set; } = string.Empty;
        [StringLength(10)] public string? Platform { get; set; }
        [StringLength(20)] public string? AppVersion { get; set; }
        public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>An announcement whose push notification has gone (or was skipped) - each is pushed once.</summary>
    [Table("AppPushLog")]
    public class AppPushLog
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int AnnouncementId { get; set; }
        /// <summary>sent | skipped | failed</summary>
        [StringLength(10)] public string Status { get; set; } = "sent";
        [StringLength(500)] public string? Detail { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Every feature an app role can be given; the Mobile App Permissions page lists these.</summary>
    public static class AppFeatures
    {
        public const string ClassView = "app.class.view";
        public const string ClassAttendance = "app.class.attendance";
        public const string ClassDiary = "app.class.diary";
        public const string ClassMarks = "app.class.marks";
        public const string AdminDashboard = "app.admin.dashboard";
        public const string AdminStudents = "app.admin.students";
        public const string AdminStaff = "app.admin.staff";
        public const string AnnounceSend = "app.announce.send";

        public static readonly (string Key, string Title, string Description)[] All =
        {
            (ClassView,       "View my classes",        "Students, attendance and results of the classes the teacher is allocated to"),
            (ClassAttendance, "Mark class attendance",  "Mark the daily attendance of those classes from the app"),
            (ClassDiary,      "Upload daily diary",     "Photograph and upload the class diary"),
            (ClassMarks,      "Enter marks",            "Enter exam marks for the teacher's own subjects"),
            (AdminDashboard,  "School dashboard",       "Whole-school figures: students, attendance, fee collection, staff"),
            (AdminStudents,   "Any student",            "Search and open any student's profile, fees, attendance and results"),
            (AdminStaff,      "Staff",                  "Staff list and today's staff attendance"),
            (AnnounceSend,    "Send announcements",     "Publish an announcement with a push notification"),
        };
    }
}

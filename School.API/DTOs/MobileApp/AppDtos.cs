namespace School.API.DTOs.MobileApp
{
    // ── sign in ──────────────────────────────────────────────────────────
    public class AppLoginDto
    {
        /// <summary>student | parent | staff</summary>
        public string Mode { get; set; } = "parent";
        public string LoginId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AppLoginResultDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool MustChangePassword { get; set; }
        public AppMeDto Me { get; set; } = new();
    }

    public class AppChangePasswordDto
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class AppSchoolInfoDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? LogoUrl { get; set; }
        /// <summary>What parents type on first launch to find this school (App:SchoolCode).</summary>
        public string? SchoolCode { get; set; }
        /// <summary>The oldest app version this server still works with.</summary>
        public string MinAppVersion { get; set; } = "1.0.0";
        /// <summary>The newest APK on the download page; an older app offers to update.</summary>
        public string LatestVersion { get; set; } = "1.0.0";
        /// <summary>Where the APK is downloaded (the school's web site, not the Play Store).</summary>
        public string? DownloadUrl { get; set; }
    }

    // ── who is signed in ─────────────────────────────────────────────────
    public class AppMeDto
    {
        public int AccountId { get; set; }
        public string Mode { get; set; } = "parent";
        public string Name { get; set; } = string.Empty;
        public string LoginId { get; set; } = string.Empty;
        public string? RoleName { get; set; }
        public bool IsAdmin { get; set; }
        public List<string> Features { get; set; } = new();

        /// <summary>Student: themself. Parent: every child. Staff: their own children at the school.</summary>
        public List<AppStudentCardDto> Students { get; set; } = new();
        public AppStaffCardDto? Staff { get; set; }

        /// <summary>Push topics this phone subscribes to (AppTopics); re-read at every launch.</summary>
        public List<string> Topics { get; set; } = new();
    }

    public class AppStudentCardDto
    {
        public int StudentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? RollNo { get; set; }
        public string? FatherName { get; set; }
        public int CampusId { get; set; }
        public string? CampusName { get; set; }
        public int ClassId { get; set; }
        public string? ClassName { get; set; }
        public int SectionId { get; set; }
        public string? SectionName { get; set; }
        public string? Gender { get; set; }
    }

    public class AppStaffCardDto
    {
        public int EmployeeId { get; set; }
        public string? EmployeeCode { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Designation { get; set; }
        public int? CampusId { get; set; }
        public string? CampusName { get; set; }
        public bool HasPhoto { get; set; }
    }

    // ── a student's data ─────────────────────────────────────────────────
    public class AppStudentSummaryDto
    {
        public AppStudentCardDto Student { get; set; } = new();
        /// <summary>P present, T late, A absent, L leave; null = not marked yet.</summary>
        public string? TodayStatus { get; set; }
        public int MonthPresent { get; set; }
        public int MonthAbsent { get; set; }
        public int MonthLeave { get; set; }
        public decimal? MonthPercent { get; set; }
        public decimal DuesTotal { get; set; }
        public int DuesMonths { get; set; }
        public string? LastExamName { get; set; }
        public decimal? LastExamPercent { get; set; }
        public int DiaryPagesToday { get; set; }
    }

    public class AppAttendanceMonthDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public List<AppAttendanceDayDto> Days { get; set; } = new();
        public int Present { get; set; }
        public int Late { get; set; }
        public int Absent { get; set; }
        public int Leave { get; set; }
        public decimal? Percent { get; set; }
    }

    public class AppAttendanceDayDto
    {
        public DateTime Date { get; set; }
        /// <summary>P, T (late), A, L; H holiday; S Sunday; "" not marked.</summary>
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
    }

    public class AppFeesDto
    {
        public decimal DuesTotal { get; set; }
        public List<AppDueDto> Dues { get; set; } = new();
        public AppVoucherDto? LastVoucher { get; set; }
        public decimal? LedgerBalance { get; set; }
    }

    public class AppDueDto
    {
        public DateTime Month { get; set; }
        public string? FeeType { get; set; }
        public decimal Amount { get; set; }
    }

    public class AppVoucherDto
    {
        public string? VoucherNo { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentMethod { get; set; }
    }

    public class AppLedgerRowDto
    {
        public DateTime Date { get; set; }
        public string? Description { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
    }

    public class AppExamDto
    {
        public int ExamId { get; set; }
        public string ExamName { get; set; } = string.Empty;
        public decimal TotalMarks { get; set; }
        public decimal ObtainedMarks { get; set; }
        public decimal? Percent { get; set; }
        public int FailedSubjects { get; set; }
        public DateTime? FirstPaper { get; set; }
    }

    public class AppExamSubjectDto
    {
        public string SubjectName { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public decimal TotalMarks { get; set; }
        public decimal PassMarks { get; set; }
        public decimal? Obtained { get; set; }
        public bool Absent { get; set; }
        public bool Passed { get; set; }
    }

    public class AppDiaryDto
    {
        public int DiaryId { get; set; }
        public DateTime Date { get; set; }
        public string? Notes { get; set; }
        public List<int> ImageIds { get; set; } = new();
    }

    public class AppTimingDto
    {
        public string TimeIn { get; set; } = string.Empty;
        public string TimeOut { get; set; } = string.Empty;
        public string? FridayIn { get; set; }
        public string? FridayOut { get; set; }
    }

    public class AppAnnouncementDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool HasImage { get; set; }
    }

    public class AppPostAnnouncementDto
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        /// <summary>0 everyone, 1 parents only, 2 staff only.</summary>
        public byte Audience { get; set; }
        /// <summary>Admin only; anyone else's goes to their own campus.</summary>
        public int? CampusId { get; set; }
        /// <summary>Last day it is shown; null = until switched off on the web.</summary>
        public DateTime? EndDate { get; set; }
        public School.API.DTOs.Academic.UploadImageDto? Image { get; set; }
    }

    // ── chat (the WhatsApp menu inside the app) ──────────────────────────
    public class AppChatSendDto
    {
        /// <summary>What the user typed, or the id of the menu row they picked ("1", "2"...).</summary>
        public string Text { get; set; } = string.Empty;
    }

    public class AppChatMessageDto
    {
        /// <summary>WhatsApp formatting (*bold*, _italic_); a menu's body; a document's caption.</summary>
        public string Text { get; set; } = string.Empty;
        public AppChatMenuDto? Menu { get; set; }
        public AppChatFileDto? File { get; set; }
    }

    public class AppChatMenuDto
    {
        public string? Header { get; set; }
        public string? Footer { get; set; }
        public string ButtonText { get; set; } = "Menu";
        public List<AppChatMenuRowDto> Rows { get; set; } = new();
    }

    public class AppChatMenuRowDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class AppChatFileDto
    {
        /// <summary>Download from GET api/app/chat/files/{Id} within 30 minutes.</summary>
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        /// <summary>pdf | image</summary>
        public string Kind { get; set; } = "pdf";
        public int Size { get; set; }
    }

    // ── staff ────────────────────────────────────────────────────────────
    public class AppStaffSummaryDto
    {
        public AppStaffCardDto Staff { get; set; } = new();
        public string? TodayStatus { get; set; }
        public string? TodayIn { get; set; }
        public string? TodayOut { get; set; }
        public int MonthPresent { get; set; }
        public int MonthAbsent { get; set; }
        public int MonthLeave { get; set; }
        public int MonthLateMinutes { get; set; }
        public AppPayslipDto? LastPayslip { get; set; }
        public int ClassCount { get; set; }
    }

    public class AppStaffDayDto
    {
        public DateTime Date { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? TimeIn { get; set; }
        public string? TimeOut { get; set; }
        public int LateMinutes { get; set; }
        public int EarlyMinutes { get; set; }
    }

    public class AppPayslipDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Gross { get; set; }
        public decimal Deduction { get; set; }
        public decimal TakeHome { get; set; }
        public bool IsFinal { get; set; }
        public DateTime? PaidDate { get; set; }
    }

    public class AppClassDto
    {
        public int ClassId { get; set; }
        public string? ClassName { get; set; }
        public int SectionId { get; set; }
        public string? SectionName { get; set; }
        public List<string> Subjects { get; set; } = new();
        public int Students { get; set; }
    }

    public class AppClassAttendanceDto
    {
        public DateTime Date { get; set; }
        public bool Marked { get; set; }
        public List<AppClassAttendanceRowDto> Rows { get; set; } = new();
    }

    public class AppClassAttendanceRowDto
    {
        public int StudentId { get; set; }
        public string? RollNo { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? FatherName { get; set; }
        /// <summary>P, T, A, L or "" when not marked.</summary>
        public string Status { get; set; } = string.Empty;
    }

    public class AppSaveClassAttendanceDto
    {
        public DateTime Date { get; set; }
        public List<AppClassAttendanceRowDto> Rows { get; set; } = new();
    }

    public class AppDeviceDto
    {
        public string Token { get; set; } = string.Empty;
        public string? Platform { get; set; }
        public string? AppVersion { get; set; }
    }
}

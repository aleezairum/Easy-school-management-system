// DTOs/SMS/WaBotDtos.cs
//
// Plain carriers for the WhatsApp auto-reply lookups. These are read with
// ADO.NET rather than EF — the same approach WaChatRepository already uses for
// its contact lookup — so none of them needs registering as a keyless entity.
namespace School.API.DTOs.SMS
{
    public class BotStudentDto
    {
        public int StudentId { get; set; }
        public int? ParentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? RollNo { get; set; }

        /// <summary>
        /// Boys or Girls campus. Needed because timings and fees are held per
        /// campus — Class 5 does not run to the same clock on both.
        /// </summary>
        public int CampusId { get; set; }

        public int ClassId { get; set; }
        public int SectionId { get; set; }
        public string? ClassName { get; set; }
        public string? SectionName { get; set; }
        public string? FatherName { get; set; }

        public string ClassLabel =>
            string.Join(" - ", new[] { ClassName, SectionName }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    /// <summary>A member of staff reachable on a WhatsApp number.</summary>
    public class BotStaffDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? DesignationName { get; set; }
        public int? CampusId { get; set; }
        public string? CampusName { get; set; }
    }

    /// <summary>One page of a class diary, picture included.</summary>
    public class BotDiaryPageDto
    {
        public int DiaryId { get; set; }
        public string? Notes { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "image/jpeg";
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }

    public class BotAnnouncementDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? ImageFileName { get; set; }
        public byte[]? ImageData { get; set; }
    }

    public class BotDueDto
    {
        public DateTime VDate { get; set; }
        public string? FeeTypeName { get; set; }
        public decimal PendingAmount { get; set; }
    }

    public class BotVoucherDto
    {
        // The voucher header id — the bot re-renders the real A4 voucher from it.
        public decimal Vid { get; set; }
        public string? VNo { get; set; }
        public DateTime VDate { get; set; }
        public decimal PaidAmount { get; set; }
        public string? PaymentMethod { get; set; }
        public List<string> Lines { get; set; } = new();
    }

    public class BotLedgerSummaryDto
    {
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        // Signed: positive is owed to the school, negative is paid in advance.
        public decimal ClosingBalance { get; set; }
        public List<string> RecentLines { get; set; } = new();
    }

    public class BotAttendanceDto
    {
        public int Present { get; set; }
        public int Absent { get; set; }
        public int Leave { get; set; }
        public int Total => Present + Absent + Leave;
        public decimal Percentage => Total == 0 ? 0 : Math.Round(Present * 100m / Total, 1);
    }

    // Today's mark for one student. A null Code means the register has not
    // been filled for that date yet — a holiday, or simply early in the day —
    // which is a different thing from being marked absent and must read that way.
    public class BotTodayAttendanceDto
    {
        public int? Code { get; set; }        // 1 = Present, 0 = Absent, 2 = Leave
        public bool IsLate { get; set; }
        public string? Remarks { get; set; }

        public string Status => Code switch
        {
            1 => IsLate ? "Present (Late)" : "Present",
            0 => "Absent",
            2 => "Leave",
            _ => "Not marked yet"
        };
    }

    public class BotResultDto
    {
        // Needed to re-render the official result card for this exam.
        public int ExamId { get; set; }
        public string? ExamName { get; set; }
        public string? SubjectName { get; set; }
        public decimal TotalMarks { get; set; }
        public decimal ObtainedMarks { get; set; }
    }

    // ── School timings ───────────────────────────────────────────────────
    //
    // From SMSClassTiming — the class-wise STUDENT timing the office enters on
    // the Class Timings screen. Deliberately not HrSchoolTiming: that one is the
    // staff calendar behind employee attendance and payroll, and a parent told
    // the teachers' hours would send their child at the wrong time.
    public class BotClassTimingDto
    {
        public int CampusId { get; set; }
        public int ClassId { get; set; }

        public TimeSpan TimeIn { get; set; }
        public TimeSpan TimeOut { get; set; }

        /// <summary>Fridays run on these instead, when both are set.</summary>
        public TimeSpan? FridayTimeIn { get; set; }
        public TimeSpan? FridayTimeOut { get; set; }

        /// <summary>
        /// What "the same timing" means when deciding whether two children can
        /// be told once instead of twice. Campus and class are left out on
        /// purpose: a parent with one child in Class 3 Boys and another in
        /// Class 4 Girls who both start at 7:30 wants to read that once.
        /// </summary>
        public string Signature => $"{TimeIn}|{TimeOut}|{FridayTimeIn}|{FridayTimeOut}";
    }

    // ── Fee structure ────────────────────────────────────────────────────
    //
    // The "Dues Detail" sheet the school hands out at admission time, read back
    // out of SMSFeeStructure so the printed sheet and the WhatsApp copy cannot
    // drift apart. Shaped campus -> fee type -> class -> grade, which is the
    // order the sheet itself is laid out in.

    public class BotFeeStructureDto
    {
        public string SessionName { get; set; } = string.Empty;
        public DateTime? SessionFrom { get; set; }
        public DateTime? SessionTo { get; set; }

        /// <summary>Highest band first, matching the printed sheet.</summary>
        public List<BotFeeGradeDto> Grades { get; set; } = new();

        public List<BotFeeCampusDto> Campuses { get; set; } = new();

        public bool HasData => Campuses.Any(c => c.Schedules.Count > 0);
    }

    public class BotFeeGradeDto
    {
        public int GradeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal MinPercentage { get; set; }
        public decimal MaxPercentage { get; set; }

        /// <summary>
        /// The band as the sheet words it — "90% or above", "80% to less than
        /// 90%", "less than 40%". Top and bottom read differently because there
        /// is nothing above or below them to bound against.
        /// </summary>
        public string Band =>
            MaxPercentage >= 100 ? $"{Trim(MinPercentage)}% or above"
            : MinPercentage <= 0 ? $"less than {Trim(MaxPercentage)}%"
            : $"{Trim(MinPercentage)}% to less than {Trim(MaxPercentage)}%";

        private static string Trim(decimal v) => v.ToString("0.##");
    }

    public class BotFeeCampusDto
    {
        public int CampusId { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<BotFeeScheduleDto> Schedules { get; set; } = new();
    }

    /// <summary>One fee type — one table on the sheet.</summary>
    public class BotFeeScheduleDto
    {
        public int FeeTypeId { get; set; }
        public string FeeTypeName { get; set; } = string.Empty;

        /// <summary>"Once" or "Monthly", straight from SMSFeeType.</summary>
        public string? Frequency { get; set; }

        /// <summary>
        /// Some row carries a grade, so this table is a grade x class matrix.
        /// A fee that is the same whatever the child scored (admission fee,
        /// academy fee) comes back false and prints as a single column.
        /// </summary>
        public bool IsGradeWise { get; set; }

        public List<BotFeeClassRowDto> Classes { get; set; } = new();
    }

    public class BotFeeClassRowDto
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;

        /// <summary>Grade id to amount; key 0 is the flat, gradeless amount.</summary>
        public Dictionary<int, decimal> Amounts { get; set; } = new();

        /// <summary>
        /// The fee vector as text, so neighbouring classes charging exactly the
        /// same can be merged into one row ("Nursery to 3") the way the printed
        /// sheet does.
        /// </summary>
        public string Signature =>
            string.Join("|", Amounts.OrderBy(a => a.Key).Select(a => $"{a.Key}:{a.Value}"));
    }
}

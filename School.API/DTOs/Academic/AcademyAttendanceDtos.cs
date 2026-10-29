namespace School.API.DTOs.Academic
{
    /// <summary>One academy student on the Mark Academy Attendance form, with the day's mark if any.</summary>
    public class AcademyAttendanceRowDto
    {
        public int StudentId { get; set; }
        public string? RollNo { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? FatherName { get; set; }
        public int SectionId { get; set; }
        public string? SectionName { get; set; }
        /// <summary>1 Present, 0 Absent, 2 Leave; null = not marked for this day yet.</summary>
        public int? Attendence { get; set; }
        public string? Remarks { get; set; }
    }

    public class SaveAcademyAttendanceDto
    {
        public DateTime Date { get; set; }
        public List<AcademyAttendanceItemDto> Items { get; set; } = new();
    }

    public class AcademyAttendanceItemDto
    {
        public int StudentId { get; set; }
        public int Attendence { get; set; }
        public string? Remarks { get; set; }
    }
}

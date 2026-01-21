namespace School.API.DTOs
{
    public class TimeTableDto
    {
        public int Id { get; set; }
        public int ClassId { get; set; }
        public string? ClassName { get; set; }
        public int SectionId { get; set; }
        public string? SectionName { get; set; }
        public int SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public int TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public int SessionId { get; set; }
        public string? SessionName { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public string DayName => DayOfWeek.ToString();
        public int PeriodNumber { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? RoomNumber { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class TimeTableListDto
    {
        public int Id { get; set; }
        public string? ClassName { get; set; }
        public string? SectionName { get; set; }
        public string? SubjectName { get; set; }
        public string? TeacherName { get; set; }
        public string DayName { get; set; } = string.Empty;
        public int PeriodNumber { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public string? RoomNumber { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateTimeTableDto
    {
        public int ClassId { get; set; }
        public int SectionId { get; set; }
        public int SubjectId { get; set; }
        public int TeacherId { get; set; }
        public int SessionId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public int PeriodNumber { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? RoomNumber { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateTimeTableDto
    {
        public int ClassId { get; set; }
        public int SectionId { get; set; }
        public int SubjectId { get; set; }
        public int TeacherId { get; set; }
        public int SessionId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public int PeriodNumber { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? RoomNumber { get; set; }
        public bool IsActive { get; set; }
    }
}

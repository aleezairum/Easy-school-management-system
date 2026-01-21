using School.API.Data.DBModels.Academic;

namespace School.API.DTOs
{
    public class AttendanceDto
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? RollNumber { get; set; }
        public int ClassId { get; set; }
        public string? ClassName { get; set; }
        public int SectionId { get; set; }
        public string? SectionName { get; set; }
        public int SessionId { get; set; }
        public string? SessionName { get; set; }
        public DateTime AttendanceDate { get; set; }
        public AttendanceStatus Status { get; set; }
        public string StatusName => Status.ToString();
        public TimeSpan? TimeIn { get; set; }
        public TimeSpan? TimeOut { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class AttendanceListDto
    {
        public int Id { get; set; }
        public string? StudentName { get; set; }
        public string? RollNumber { get; set; }
        public string? ClassName { get; set; }
        public string? SectionName { get; set; }
        public DateTime AttendanceDate { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public AttendanceStatus Status { get; set; }
        public string? TimeIn { get; set; }
        public string? TimeOut { get; set; }
    }

    public class CreateAttendanceDto
    {
        public int StudentId { get; set; }
        public int ClassId { get; set; }
        public int SectionId { get; set; }
        public int SessionId { get; set; }
        public DateTime AttendanceDate { get; set; }
        public AttendanceStatus Status { get; set; }
        public TimeSpan? TimeIn { get; set; }
        public TimeSpan? TimeOut { get; set; }
        public string? Remarks { get; set; }
    }

    public class UpdateAttendanceDto
    {
        public int StudentId { get; set; }
        public int ClassId { get; set; }
        public int SectionId { get; set; }
        public int SessionId { get; set; }
        public DateTime AttendanceDate { get; set; }
        public AttendanceStatus Status { get; set; }
        public TimeSpan? TimeIn { get; set; }
        public TimeSpan? TimeOut { get; set; }
        public string? Remarks { get; set; }
    }

    public class BulkAttendanceDto
    {
        public int ClassId { get; set; }
        public int SectionId { get; set; }
        public int SessionId { get; set; }
        public DateTime AttendanceDate { get; set; }
        public List<StudentAttendanceDto> Students { get; set; } = new();
    }

    public class StudentAttendanceDto
    {
        public int StudentId { get; set; }
        public AttendanceStatus Status { get; set; }
        public TimeSpan? TimeIn { get; set; }
        public TimeSpan? TimeOut { get; set; }
        public string? Remarks { get; set; }
    }

    public class AttendanceSummaryDto
    {
        public int StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? RollNumber { get; set; }
        public int TotalDays { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LateDays { get; set; }
        public int LeaveDays { get; set; }
        public decimal AttendancePercentage { get; set; }
    }
}

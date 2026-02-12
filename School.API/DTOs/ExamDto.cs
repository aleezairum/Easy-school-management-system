using School.API.Data.DBModels.Academic;

namespace School.API.DTOs
{
    public class ExamDto
    {
        public int Id { get; set; }
        public string ExamName { get; set; } = string.Empty;
        public ExamType ExamType { get; set; }
        public string ExamTypeName => ExamType.ToString();
        public int ClassId { get; set; }
        public string? ClassName { get; set; }
        public int SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public int SessionId { get; set; }
        public string? SessionName { get; set; }
        public DateTime ExamDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public decimal TotalMarks { get; set; }
        public decimal PassingMarks { get; set; }
        public ExamStatus Status { get; set; }
        public string StatusName => Status.ToString();
        public string? Description { get; set; }
        public string? RoomNumber { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ExamListDto
    {
        public int Id { get; set; }
        public string ExamName { get; set; } = string.Empty;
        public string ExamTypeName { get; set; } = string.Empty;
        public string? ClassName { get; set; }
        public string? SubjectName { get; set; }
        public DateTime ExamDate { get; set; }
        public decimal TotalMarks { get; set; }
        public decimal PassingMarks { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class CreateExamDto
    {
        public string ExamName { get; set; } = string.Empty;
        public ExamType ExamType { get; set; }
        public int ClassId { get; set; }
        public int SubjectId { get; set; }
        public int SessionId { get; set; }
        public DateTime ExamDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public decimal TotalMarks { get; set; }
        public decimal PassingMarks { get; set; }
        public ExamStatus Status { get; set; } = ExamStatus.Scheduled;
        public string? Description { get; set; }
        public string? RoomNumber { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateExamDto
    {
        public string ExamName { get; set; } = string.Empty;
        public ExamType ExamType { get; set; }
        public int ClassId { get; set; }
        public int SubjectId { get; set; }
        public int SessionId { get; set; }
        public DateTime ExamDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public decimal TotalMarks { get; set; }
        public decimal PassingMarks { get; set; }
        public ExamStatus Status { get; set; }
        public string? Description { get; set; }
        public string? RoomNumber { get; set; }
        public bool IsActive { get; set; }
    }

    public class ExamDropdownDto
    {
        public int Id { get; set; }
        public string ExamName { get; set; } = string.Empty;
        public string? SubjectName { get; set; }
        public string DisplayText => $"{ExamName} - {SubjectName}";
    }

    // Exam Result DTOs
    public class ExamResultDto
    {
        public int Id { get; set; }
        public int ExamId { get; set; }
        public string? ExamName { get; set; }
        public int StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? RollNumber { get; set; }
        public decimal? MarksObtained { get; set; }
        public string? Grade { get; set; }
        public bool IsAbsent { get; set; }
        public bool IsPassed { get; set; }
        public string? Remarks { get; set; }
        public decimal TotalMarks { get; set; }
        public decimal PassingMarks { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ExamResultListDto
    {
        public int Id { get; set; }
        public string? StudentName { get; set; }
        public string? RollNumber { get; set; }
        public decimal? MarksObtained { get; set; }
        public decimal TotalMarks { get; set; }
        public string? Grade { get; set; }
        public bool IsAbsent { get; set; }
        public bool IsPassed { get; set; }
    }

    public class CreateExamResultDto
    {
        public int ExamId { get; set; }
        public int StudentId { get; set; }
        public decimal? MarksObtained { get; set; }
        public string? Grade { get; set; }
        public bool IsAbsent { get; set; }
        public string? Remarks { get; set; }
    }

    public class BulkExamResultDto
    {
        public int ExamId { get; set; }
        public List<StudentExamResultDto> Results { get; set; } = new();
    }

    public class StudentExamResultDto
    {
        public int StudentId { get; set; }
        public decimal? MarksObtained { get; set; }
        public string? Grade { get; set; }
        public bool IsAbsent { get; set; }
        public string? Remarks { get; set; }
    }
}

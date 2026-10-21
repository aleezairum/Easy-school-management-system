namespace School.API.DTOs.Academic
{
    /// <summary>An exam whose result the office has finalised.</summary>
    public class ExamFinalizeDto
    {
        public int ExamId { get; set; }
        public DateTime FinalizedDate { get; set; }
        public int? FinalizedBy { get; set; }
    }

    /// <summary>One student on the Result Exclude List screen.</summary>
    public class ExamExcludeRowDto
    {
        public int StudentId { get; set; }
        public string RollNo { get; set; } = "";
        public string Name { get; set; } = "";
        public string? FatherName { get; set; }
        public string? ClassName { get; set; }
        public string? SectionName { get; set; }
        public bool Excluded { get; set; }
        public string? Reason { get; set; }
    }

    public class ExamExcludeItemDto
    {
        public int StudentId { get; set; }
        public bool Excluded { get; set; }
        public string? Reason { get; set; }
    }

    public class SaveExamExcludeDto
    {
        public int ExamId { get; set; }
        public List<ExamExcludeItemDto> Items { get; set; } = new();
    }
}

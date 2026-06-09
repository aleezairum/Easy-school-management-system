namespace School.API.DTOs.Academic
{
    public class SMSExamResultSaveDto
    {
        public int VID { get; set; }
        public int ExamID { get; set; }
        public string? ExamName { get; set; }
        public int ClassID { get; set; }
        public string? ClassName { get; set; }
        public int SectionID { get; set; }
        public string? SectionName { get; set; }
        public int SubjectID { get; set; }
        public string? SubjectName { get; set; }
        public int StudentID { get; set; }
        public DateTime? VDate { get; set; }
        public decimal? TotalMarks { get; set; }
        public decimal? PassMarks { get; set; }
        public decimal? MarksObtained { get; set; }
        public string? Grade { get; set; }
        public bool IsAbsent { get; set; }
        public bool IsPassed { get; set; }
        public string? Remarks { get; set; }
        public bool? IsActive { get; set; }
        public int? InsertedBy { get; set; }
        public DateTime? InsertedDate { get; set; }
        public string? InsertedIp { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedIp { get; set; }
    }

    public class SMSExamResultBulkDto
    {
        public int ExamID { get; set; }
        public int ClassID { get; set; }
        public int SectionID { get; set; }
        public int SubjectID { get; set; }
        public decimal? TotalMarks { get; set; }
        public decimal? PassMarks { get; set; }
        public List<SMSExamResultSaveDto> Results { get; set; } = new();
    }

    // ---- Result Sheet report (output of SpRPT_SMSExamResultSheet) ----

    public class ExamResultSheetDto
    {
        // Column definitions (one per subject) — drives the header + max/pass rows
        public List<ResultSheetSubjectDto> Subjects { get; set; } = new();
        // One row per student, with a per-subject marks map keyed by subject name
        public List<ResultSheetStudentDto> Students { get; set; } = new();
    }

    public class ResultSheetSubjectDto
    {
        public int SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public decimal? TotalMarks { get; set; }
        public decimal? PassMarks { get; set; }
    }

    public class ResultSheetStudentDto
    {
        public string? RollNo { get; set; }
        public string? StudentName { get; set; }
        public string? FatherName { get; set; }
        public int? Position { get; set; }
        public decimal? CurrentPct { get; set; }
        public decimal? PrevPct { get; set; }
        public string? Result { get; set; }
        public decimal? Total { get; set; }
        // Marks per subject, keyed by SubjectName (matches Subjects[].SubjectName)
        public Dictionary<string, decimal?> Marks { get; set; } = new();
    }

    // ---- Result Card report (output of SpRPT_SMSExamResultCard) ----

    public class ExamResultCardDto
    {
        // One card per student (each carries its own per-subject detail rows)
        public List<ResultCardStudentDto> Students { get; set; } = new();
    }

    public class ResultCardStudentDto
    {
        public int StudentId { get; set; }
        public string? RollNo { get; set; }
        public string? StudentName { get; set; }
        public string? FatherName { get; set; }
        public string? ClassName { get; set; }
        public string? SectionName { get; set; }
        public int? Position { get; set; }
        public int TotalStudents { get; set; }
        public decimal? HighestMarks { get; set; }   // highest obtained marks in the class
        public decimal? CurrentPct { get; set; }
        public decimal? PrevPct { get; set; }
        public decimal? GrandTotal { get; set; }      // sum of subject max marks
        public decimal? GrandPass { get; set; }       // sum of subject pass marks
        public decimal? GrandObt { get; set; }        // total obtained
        public string? GrandStatus { get; set; }
        public int PresentDays { get; set; }           // days present in attendance window
        public int SchoolDays { get; set; }            // days school was held in the window
        public decimal? AttendancePct { get; set; }    // PresentDays / SchoolDays %
        public List<ResultCardSubjectDto> Subjects { get; set; } = new();
    }

    public class ResultCardSubjectDto
    {
        public string? SubjectName { get; set; }
        public DateTime? VDate { get; set; }
        public decimal? TotalMarks { get; set; }
        public decimal? PassMarks { get; set; }
        public decimal? MarksObtained { get; set; }
        public string? Status { get; set; }
    }

    // ---- Position Wise report (output of SpRPT_SMSExamStudentPosition) ----

    // ---- Result Announcement report (output of SpRPT_SMSExamResultAnnouncement) ----

    public class ExamAnnouncementDto
    {
        public ResultStatRowDto Overall { get; set; } = new();
        public List<ResultStatRowDto> Campuses { get; set; } = new();
        public List<ResultStatRowDto> Classes { get; set; } = new();
        public List<AnnouncementTopRowDto> TopPositions { get; set; } = new();
    }

    public class ResultStatRowDto
    {
        public string? Name { get; set; }   // campus / class name (null for overall)
        public int TotalStudents { get; set; }
        public int TotalPass { get; set; }
        public int TotalFail { get; set; }
        public decimal? PassPct { get; set; }
        public decimal? FailPct { get; set; }
    }

    public class AnnouncementTopRowDto
    {
        public string? ClassName { get; set; }
        public int? Position { get; set; }
        public string? RollNo { get; set; }
        public string? StudentName { get; set; }
        public string? FatherName { get; set; }
        public string? SectionName { get; set; }
        public decimal? TotalMarks { get; set; }
        public decimal? ObtainedMarks { get; set; }
        public decimal? Percent { get; set; }
    }

    // ---- Executive (summary) report (output of SpRPT_SMSExamExecutive) ----

    public class ExamExecutiveDto
    {
        public ExecutiveOverallDto Overall { get; set; } = new();
        public List<ExecutiveSubjectDto> Subjects { get; set; } = new();
    }

    public class ExecutiveOverallDto
    {
        public int TotalStudents { get; set; }
        public int TotalPass { get; set; }
        public int TotalFail { get; set; }
        public decimal? ResultPct { get; set; }
    }

    public class ExecutiveSubjectDto
    {
        public string? SubjectName { get; set; }
        public int FailCount { get; set; }
        public decimal? FailPct { get; set; }
        public int PassCount { get; set; }
        public decimal? PassPct { get; set; }
        public string? TeacherName { get; set; }
        public decimal? Obtained { get; set; }
        public decimal? Total { get; set; }
        public decimal? Percent { get; set; }
    }

    public class ExamPositionRowDto
    {
        public int Sr { get; set; }
        public int? Position { get; set; }          // null = did not pass all subjects
        public string? RollNo { get; set; }
        public string? StudentName { get; set; }
        public string? FatherName { get; set; }
        public string? SectionName { get; set; }
        public decimal? TotalMarks { get; set; }    // grand total (sum of subject max marks)
        public decimal? ObtainedMarks { get; set; } // grand obtained
        public decimal? Percent { get; set; }
        public string? Status { get; set; }         // "Pass" / "Fail"
    }
}

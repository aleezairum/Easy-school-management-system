namespace School.API.DTOs.Academic
{
    // One assigned subject row (output of SpGet_SMSStaffSubject, with joined names)
    public class StaffSubjectRowDto
    {
        public int Vid { get; set; }
        public int ExamId { get; set; }
        public string? ExamName { get; set; }
        public int TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public int ClassId { get; set; }
        public string? ClassName { get; set; }
        public int SectionId { get; set; }
        public string? SectionName { get; set; }
        public int SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public int? LectureNo { get; set; }
        public bool IsActive { get; set; }
    }

    // Single record save (VID = 0 -> insert, > 0 -> update)
    public class StaffSubjectDto
    {
        public int VID { get; set; }
        public int ExamId { get; set; }      // assignments are per exam
        public int StaffId { get; set; }     // = TeacherID
        public int ClassId { get; set; }
        public int SectionId { get; set; }
        public int SubjectId { get; set; }
        public int? LectureNo { get; set; }
        public bool? IsActive { get; set; }
    }

    /// <summary>
    /// "Copy from previous exam": carry every assignment of one exam into another.
    /// </summary>
    /// <remarks>
    /// Additive, never destructive — the target exam keeps what it already has,
    /// and rows it already carries are skipped, so copying twice is a no-op.
    /// </remarks>
    public class CopyStaffSubjectDto
    {
        public int FromExamId { get; set; }
        public int ToExamId { get; set; }
    }
}

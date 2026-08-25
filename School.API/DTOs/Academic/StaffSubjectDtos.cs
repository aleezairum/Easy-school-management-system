namespace School.API.DTOs.Academic
{
    // One assigned subject row (output of SpGet_SMSStaffSubject, with joined names)
    public class StaffSubjectRowDto
    {
        public int Vid { get; set; }
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
        public int StaffId { get; set; }     // = TeacherID
        public int ClassId { get; set; }
        public int SectionId { get; set; }
        public int SubjectId { get; set; }
        public int? LectureNo { get; set; }
        public bool? IsActive { get; set; }
    }
}

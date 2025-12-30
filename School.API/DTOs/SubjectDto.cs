namespace School.API.DTOs
{
    public class SubjectDto
    {
        public int VID { get; set; }
        public string VName { get; set; } = string.Empty;
        public int ClassID { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class CreateSubjectDto
    {
        public string VName { get; set; } = string.Empty;
        public int ClassID { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateSubjectDto
    {
        public int VID { get; set; }
        public string VName { get; set; } = string.Empty;
        public int ClassID { get; set; }
        public bool IsActive { get; set; }
    }
}

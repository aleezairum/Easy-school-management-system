namespace School.API.DTOs
{
    public class SectionDto
    {
        public int VID { get; set; }
        public string VName { get; set; } = string.Empty;
        public int ClassID { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class CreateSectionDto
    {
        public string VName { get; set; } = string.Empty;
        public int ClassID { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateSectionDto
    {
        public int VID { get; set; }
        public string VName { get; set; } = string.Empty;
        public int ClassID { get; set; }
        public bool IsActive { get; set; }
    }
}

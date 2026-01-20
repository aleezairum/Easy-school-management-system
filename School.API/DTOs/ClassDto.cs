namespace School.API.DTOs
{
    public class ClassDto
    {
        public int VID { get; set; }
        public string VName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class ClassDropdownDto
    {
        public int VID { get; set; }
        public string VName { get; set; } = string.Empty;
    }

    public class CreateClassDto
    {
        public string VName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class UpdateClassDto
    {
        public int VID { get; set; }
        public string VName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}

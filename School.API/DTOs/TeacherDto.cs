namespace School.API.DTOs
{
    public class TeacherDto
    {
        public int VID { get; set; }
        public string? VName { get; set; }
        public string? FatherName { get; set; }
        public int? Gender { get; set; }
        public string? CNIC { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Qualification { get; set; }
        public int? ExperienceYears { get; set; }
        public bool? IsActive { get; set; }
        public int? InsertedBy { get; set; }
        public DateTime? InsertedDate { get; set; }
        public string? InsertedIp { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedIp { get; set; }
    }
}

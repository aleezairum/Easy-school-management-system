namespace School.API.DTOs.Academic
{
    public class SMSSectionSaveDto
    {
        public int VID { get; set; }
        public string? VName { get; set; }
        public int ClassID { get; set; }
        public bool? IsActive { get; set; }
        public int? InsertedBy { get; set; }
        public DateTime? InsertedDate { get; set; }
        public string? InsertedIp { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedIp { get; set; }
    }
}

namespace School.API.DTOs.FeeManagement
{
    public class FeeTypeSaveDto
    {
        public int VID { get; set; }
        public string? VName { get; set; }
        public string Frequency { get; set; }
        public bool? IsActive { get; set; }
        public int? InsertedBy { get; set; }
        public DateTime? InsertedDate { get; set; }
        public string? InsertedIp { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedIp { get; set; }
    }
}

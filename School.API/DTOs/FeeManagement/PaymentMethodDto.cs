namespace School.API.DTOs.FeeManagement
{
    public class PaymentMethodDto
    {
        public int VID { get; set; }
        public string? VCode { get; set; }
        public string? VName { get; set; }
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
        public int? InsertedBy { get; set; }
        public DateTime? InsertedDate { get; set; }
        public string? InsertedIp { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedIp { get; set; }
    }

}

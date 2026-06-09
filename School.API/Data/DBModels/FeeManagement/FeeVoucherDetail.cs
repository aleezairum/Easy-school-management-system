using System.ComponentModel.DataAnnotations;

namespace School.API.Data.DBModels.Accounts
{
    public class FeeVoucherDetail
    {
        [Key]
        public int VID { get; set; }
        public int MID { get; set; }
        public int StudentFeeID { get; set; }
        public decimal PendingBalance { get; set; }
        public decimal PaidAmount { get; set; }
        public int? InsertedBy { get; set; }
        public DateTime InsertedDate { get; set; }
        public string? InsertedIp { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedIp { get; set; }
    }
}
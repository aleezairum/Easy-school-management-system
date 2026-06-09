using System.ComponentModel.DataAnnotations;

namespace School.API.Data.DBModels.Accounts
{
    public class FeeVoucherMain
    {
        [Key]
        public int VID { get; set; }
        public string? VNo { get; set; }
        public DateTime VDate { get; set; }
        public int PaymentMethodID { get; set; }
        public string? PaymentMethod { get; set; }
        public int ParentID { get; set; }
        public string? ParentName { get; set; }
        public decimal BalanceAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public string? Remarks { get; set; }
        public string? UserFullName { get; set; }
        public int? InsertedBy { get; set; }
        public DateTime InsertedDate { get; set; }
        public string? InsertedIp { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedIp { get; set; }
    }
}
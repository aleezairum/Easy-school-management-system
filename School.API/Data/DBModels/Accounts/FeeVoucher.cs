using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Data.DBModels.Accounts
{
    [Table("FeeVouchers")]
    public class FeeVoucher
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string VoucherNumber { get; set; } = string.Empty;

        public int ChallanVoucherId { get; set; }

        [ForeignKey("ChallanVoucherId")]
        public virtual ChallanVoucher? ChallanVoucher { get; set; }

        public int PaymentMethodId { get; set; }

        [ForeignKey("PaymentMethodId")]
        public virtual PaymentMethod? PaymentMethod { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime PaymentDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; }

        [MaxLength(50)]
        public string? ChequeNumber { get; set; }

        [Column(TypeName = "date")]
        public DateTime? ChequeDate { get; set; }

        [MaxLength(100)]
        public string? BankName { get; set; }

        [MaxLength(100)]
        public string? TransactionReference { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public int? ReceivedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using School.API.Data.DBModels.Academic;
using School.API.Models;

namespace School.API.Data.DBModels.Accounts
{
    public enum ChallanStatus
    {
        Pending = 1,
        PartiallyPaid = 2,
        Paid = 3,
        Cancelled = 4,
        Overdue = 5
    }

    [Table("ChallanVouchers")]
    public class ChallanVoucher
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ChallanNumber { get; set; } = string.Empty;

        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        public int ClassId { get; set; }

        [ForeignKey("ClassId")]
        public virtual SMSClass? Class { get; set; }

        public int SectionId { get; set; }

        [ForeignKey("SectionId")]
        public virtual SMSSection? Section { get; set; }

        public int SessionId { get; set; }

        [ForeignKey("SessionId")]
        public virtual AcademicSessionYear? Session { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime IssueDate { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime DueDate { get; set; }

        // Fee Components
        [Column(TypeName = "decimal(18,2)")]
        public decimal TuitionFee { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal AdmissionFee { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ExamFee { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TransportFee { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal LabFee { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal LibraryFee { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SportsFee { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ComputerFee { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal OtherFee { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ArrearsAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal LateFee { get; set; } = 0;

        [NotMapped]
        public decimal GrossAmount => TuitionFee + AdmissionFee + ExamFee + TransportFee +
                                      LabFee + LibraryFee + SportsFee + ComputerFee +
                                      OtherFee + ArrearsAmount;

        [NotMapped]
        public decimal NetAmount => GrossAmount - Discount + LateFee;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; } = 0;

        [NotMapped]
        public decimal BalanceAmount => TotalAmount - PaidAmount;

        [Required]
        public ChallanStatus Status { get; set; } = ChallanStatus.Pending;

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public int? ForMonth { get; set; }

        public int? ForYear { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<FeeVoucher>? FeeVouchers { get; set; }
    }
}

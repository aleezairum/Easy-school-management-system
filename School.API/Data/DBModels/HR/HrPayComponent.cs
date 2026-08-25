using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Data.DBModels.HR
{
    // One slab band, e.g. "16-30 min late -> Rs 150" or "after 3 lates -> half day".
    // Owned by HrPayComponent, stored as JSON in SlabsJson (D3).
    public class HrPayComponentSlab
    {
        public decimal FromValue { get; set; }
        public decimal? ToValue { get; set; }   // null = open-ended ("and above")
        public decimal? Amount { get; set; }
        public decimal? Days { get; set; }
        public decimal? Percent { get; set; }
    }

    [Table("HrPayComponent")]
    public class HrPayComponent
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public HrPayComponentKind Kind { get; set; }
        public HrPayCalcType CalcType { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? Rate { get; set; }

        public HrRateSource RateSource { get; set; } = HrRateSource.Fixed;
        public HrPayBaseOn? BaseOn { get; set; }

        // 30 = fixed 30, 0 = calendar days, 255 = working days — see HrDivisorDays.
        public byte? DivisorDays { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? DivisorHours { get; set; }   // null = take from the employee's shift

        public int? GraceMinutes { get; set; }

        [Column(TypeName = "decimal(9,2)")]
        public decimal? FreeUnitsPerMonth { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? CapPerDay { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? CapPerMonth { get; set; }

        // Materialized from/to SlabsJson by EF (owned, ToJson) — see SchoolDbContext.
        public List<HrPayComponentSlab> Slabs { get; set; } = new();

        [MaxLength(500)]
        public string? Formula { get; set; }

        public HrPayScopeType ScopeType { get; set; } = HrPayScopeType.Global;
        public int? ScopeId { get; set; }

        public int Priority { get; set; }
        public bool ShowOnPayslip { get; set; } = true;

        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }

        public bool IsActive { get; set; } = true;
    }
}

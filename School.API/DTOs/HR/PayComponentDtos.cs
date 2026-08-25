namespace School.API.DTOs.HR
{
    public class PayComponentSlabDto
    {
        public decimal FromValue { get; set; }
        public decimal? ToValue { get; set; }
        public decimal? Amount { get; set; }
        public decimal? Days { get; set; }
        public decimal? Percent { get; set; }
    }

    public class PayComponentDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public byte Kind { get; set; }
        public byte CalcType { get; set; }
        public decimal? Rate { get; set; }
        public byte RateSource { get; set; }
        public byte? BaseOn { get; set; }
        public byte? DivisorDays { get; set; }
        public decimal? DivisorHours { get; set; }
        public int? GraceMinutes { get; set; }
        public decimal? FreeUnitsPerMonth { get; set; }
        public decimal? CapPerDay { get; set; }
        public decimal? CapPerMonth { get; set; }
        public List<PayComponentSlabDto> Slabs { get; set; } = new();
        public string? Formula { get; set; }
        public byte ScopeType { get; set; }
        public int? ScopeId { get; set; }
        public int Priority { get; set; }
        public bool ShowOnPayslip { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool IsActive { get; set; }
    }

    public class SavePayComponentDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public byte Kind { get; set; }
        public byte CalcType { get; set; }
        public decimal? Rate { get; set; }
        public byte RateSource { get; set; } = 1;
        public byte? BaseOn { get; set; }
        public byte? DivisorDays { get; set; }
        public decimal? DivisorHours { get; set; }
        public int? GraceMinutes { get; set; }
        public decimal? FreeUnitsPerMonth { get; set; }
        public decimal? CapPerDay { get; set; }
        public decimal? CapPerMonth { get; set; }
        public List<PayComponentSlabDto> Slabs { get; set; } = new();
        public string? Formula { get; set; }
        public byte ScopeType { get; set; } = 1;
        public int? ScopeId { get; set; }
        public int Priority { get; set; }
        public bool ShowOnPayslip { get; set; } = true;
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool IsActive { get; set; } = true;
    }

    // POST /api/hr/pay-components/preview — "employee X, 25 min late -> Rs Y"
    public class PayComponentPreviewRequestDto
    {
        public int ComponentId { get; set; }
        public decimal Basic { get; set; }
        public decimal Gross { get; set; }
        public int LateMinutes { get; set; }
        public int LateCount { get; set; }
        public int AbsentDays { get; set; }
        public decimal OvertimeHours { get; set; }
        public decimal ShiftWorkHours { get; set; } = 8;
    }

    public class PayComponentPreviewResultDto
    {
        public decimal Amount { get; set; }
        public string Explanation { get; set; } = string.Empty;
    }
}

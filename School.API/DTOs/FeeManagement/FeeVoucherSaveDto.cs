namespace School.API.DTOs.FeeManagement
{
    public class FeeVoucherSaveDto
    {
        public int VID { get; set; }
        public string? VNo { get; set; }
        public DateTime VDate { get; set; }
        public int PaymentMethodID { get; set; }
        public int ParentID { get; set; }
        public decimal BalanceAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public string? Remarks { get; set; }
        public List<FeeVoucherDetailSaveDto> Details { get; set; } = new();
    }

    public class FeeVoucherDetailSaveDto
    {
        public int VID { get; set; }
        public int StudentFeeID { get; set; }
        public decimal PendingBalance { get; set; }
        public decimal PaidAmount { get; set; }
    }

    public class VNoDto
    {
        public string VNo { get; set; } = string.Empty;
    }

    public class FeeVoucherReceiptDto
    {
        public int VID { get; set; }
        public string? VNo { get; set; }
        public DateTime VDate { get; set; }
        public string? PaymentMethod { get; set; }
        public string? ParentName { get; set; }
        public decimal BalanceAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public string? Remarks { get; set; }
        public string? UserFullName { get; set; }
        public List<FeeVoucherReceiptDetailDto> Details { get; set; } = new();
    }

    public class FeeVoucherReceiptDetailDto
    {
        public int StudentFeeID { get; set; }
        public string? StudentName { get; set; }
        public string? SectionName { get; set; }
        public string? FeeTypeName { get; set; }
        public DateTime FeeMonth { get; set; }
        public decimal FeeAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal PendingBalance { get; set; }
    }
    public class FeeVoucherListDto
    {
        public int VID { get; set; }
        public string? VNo { get; set; }
        public DateTime VDate { get; set; }
        public string? PaymentMethod { get; set; }
        public int ParentID { get; set; }
        public string? ParentName { get; set; }
        public decimal BalanceAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public string? Remarks { get; set; }
        public string? UserFullName { get; set; }
        public int StudentFeeID { get; set; }
        public decimal DetailPaidAmount { get; set; }
        public decimal PendingBalance { get; set; }
        public DateTime FeeMonth { get; set; }
        public int StudentID { get; set; }
        public string? StudentName { get; set; }
        public string? SectionName { get; set; }
        public string? FeeTypeName { get; set; }
        public decimal FeeAmount { get; set; }
        public decimal ClearedAmount { get; set; }
    }
}
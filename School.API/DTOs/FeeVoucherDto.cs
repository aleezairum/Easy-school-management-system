namespace School.API.DTOs
{
    public class FeeVoucherDto
    {
        public int Id { get; set; }
        public string VoucherNumber { get; set; } = string.Empty;
        public int ChallanVoucherId { get; set; }
        public string? ChallanNumber { get; set; }
        public string? StudentName { get; set; }
        public string? RollNumber { get; set; }
        public int PaymentMethodId { get; set; }
        public string? PaymentMethodName { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal AmountPaid { get; set; }
        public string? ChequeNumber { get; set; }
        public DateTime? ChequeDate { get; set; }
        public string? BankName { get; set; }
        public string? TransactionReference { get; set; }
        public string? Remarks { get; set; }
        public int? ReceivedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class FeeVoucherListDto
    {
        public int Id { get; set; }
        public string VoucherNumber { get; set; } = string.Empty;
        public string? ChallanNumber { get; set; }
        public string? StudentName { get; set; }
        public string? RollNumber { get; set; }
        public string? PaymentMethodName { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal AmountPaid { get; set; }
        public string? TransactionReference { get; set; }
    }

    public class CreateFeeVoucherDto
    {
        public int ChallanVoucherId { get; set; }
        public int PaymentMethodId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal AmountPaid { get; set; }
        public string? ChequeNumber { get; set; }
        public DateTime? ChequeDate { get; set; }
        public string? BankName { get; set; }
        public string? TransactionReference { get; set; }
        public string? Remarks { get; set; }
        public int? ReceivedBy { get; set; }
    }

    public class UpdateFeeVoucherDto
    {
        public int ChallanVoucherId { get; set; }
        public int PaymentMethodId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal AmountPaid { get; set; }
        public string? ChequeNumber { get; set; }
        public DateTime? ChequeDate { get; set; }
        public string? BankName { get; set; }
        public string? TransactionReference { get; set; }
        public string? Remarks { get; set; }
        public int? ReceivedBy { get; set; }
    }
}

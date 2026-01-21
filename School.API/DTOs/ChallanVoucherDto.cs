using School.API.Data.DBModels.Accounts;

namespace School.API.DTOs
{
    public class ChallanVoucherDto
    {
        public int Id { get; set; }
        public string ChallanNumber { get; set; } = string.Empty;
        public int StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? RollNumber { get; set; }
        public int ClassId { get; set; }
        public string? ClassName { get; set; }
        public int SectionId { get; set; }
        public string? SectionName { get; set; }
        public int SessionId { get; set; }
        public string? SessionName { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TuitionFee { get; set; }
        public decimal AdmissionFee { get; set; }
        public decimal ExamFee { get; set; }
        public decimal TransportFee { get; set; }
        public decimal LabFee { get; set; }
        public decimal LibraryFee { get; set; }
        public decimal SportsFee { get; set; }
        public decimal ComputerFee { get; set; }
        public decimal OtherFee { get; set; }
        public decimal ArrearsAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal LateFee { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        public ChallanStatus Status { get; set; }
        public string StatusName => Status.ToString();
        public string? Remarks { get; set; }
        public int? ForMonth { get; set; }
        public int? ForYear { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ChallanVoucherListDto
    {
        public int Id { get; set; }
        public string ChallanNumber { get; set; } = string.Empty;
        public string? StudentName { get; set; }
        public string? RollNumber { get; set; }
        public string? ClassName { get; set; }
        public string? SectionName { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        public ChallanStatus Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
    }

    public class CreateChallanVoucherDto
    {
        public int StudentId { get; set; }
        public int ClassId { get; set; }
        public int SectionId { get; set; }
        public int SessionId { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TuitionFee { get; set; }
        public decimal AdmissionFee { get; set; }
        public decimal ExamFee { get; set; }
        public decimal TransportFee { get; set; }
        public decimal LabFee { get; set; }
        public decimal LibraryFee { get; set; }
        public decimal SportsFee { get; set; }
        public decimal ComputerFee { get; set; }
        public decimal OtherFee { get; set; }
        public decimal ArrearsAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal LateFee { get; set; }
        public string? Remarks { get; set; }
        public int? ForMonth { get; set; }
        public int? ForYear { get; set; }
    }

    public class UpdateChallanVoucherDto
    {
        public int StudentId { get; set; }
        public int ClassId { get; set; }
        public int SectionId { get; set; }
        public int SessionId { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TuitionFee { get; set; }
        public decimal AdmissionFee { get; set; }
        public decimal ExamFee { get; set; }
        public decimal TransportFee { get; set; }
        public decimal LabFee { get; set; }
        public decimal LibraryFee { get; set; }
        public decimal SportsFee { get; set; }
        public decimal ComputerFee { get; set; }
        public decimal OtherFee { get; set; }
        public decimal ArrearsAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal LateFee { get; set; }
        public ChallanStatus Status { get; set; }
        public string? Remarks { get; set; }
        public int? ForMonth { get; set; }
        public int? ForYear { get; set; }
    }

    public class ChallanDropdownDto
    {
        public int Id { get; set; }
        public string ChallanNumber { get; set; } = string.Empty;
        public string? StudentName { get; set; }
        public decimal BalanceAmount { get; set; }
        public string DisplayText => $"{ChallanNumber} - {StudentName} (Balance: {BalanceAmount:N2})";
    }
}

namespace School.API.DTOs
{
    public class PaymentMethodDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? BranchCode { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class PaymentMethodListDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? BankName { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreatePaymentMethodDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? BranchCode { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdatePaymentMethodDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? BranchCode { get; set; }
        public bool IsActive { get; set; }
    }

    public class PaymentMethodDropdownDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DisplayText => $"{Code} - {Name}";
    }
}

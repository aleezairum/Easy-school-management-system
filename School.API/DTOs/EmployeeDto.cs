namespace School.API.DTOs
{
    public class EmployeeDto
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; }
        public string? FirstNameUrdu { get; set; }
        public string? LastNameUrdu { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? CNIC { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime? JoiningDate { get; set; }
        public DateTime? LeavingDate { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Photo { get; set; }
        public string? Qualification { get; set; }
        public string? Specialization { get; set; }
        public string? Experience { get; set; }
        public string? Department { get; set; }

        public int? DesignationId { get; set; }
        public string? DesignationName { get; set; }

        public int? HRGradeId { get; set; }
        public string? HRGradeName { get; set; }

        public int? AcademicGradeId { get; set; }
        public string? AcademicGradeName { get; set; }

        public decimal? BasicSalary { get; set; }
        public decimal? Allowances { get; set; }
        public decimal? Deductions { get; set; }
        public decimal? NetSalary { get; set; }
        public string? BankName { get; set; }
        public string? BankAccountNo { get; set; }

        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? EmergencyContactRelation { get; set; }

        public string? EmployeeType { get; set; }
        public string? Status { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class EmployeeListDto
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? CNIC { get; set; }
        public string? Mobile { get; set; }
        public string? Photo { get; set; }
        public string? Department { get; set; }
        public string? DesignationName { get; set; }
        public string? HRGradeName { get; set; }
        public string? EmployeeType { get; set; }
        public string? Status { get; set; }
        public DateTime? JoiningDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateEmployeeDto
    {
        public string EmployeeCode { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; }
        public string? FirstNameUrdu { get; set; }
        public string? LastNameUrdu { get; set; }
        public string? CNIC { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime? JoiningDate { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Photo { get; set; }
        public string? Qualification { get; set; }
        public string? Specialization { get; set; }
        public string? Experience { get; set; }
        public string? Department { get; set; }
        public int? DesignationId { get; set; }
        public int? HRGradeId { get; set; }
        public int? AcademicGradeId { get; set; }
        public decimal? BasicSalary { get; set; }
        public decimal? Allowances { get; set; }
        public decimal? Deductions { get; set; }
        public decimal? NetSalary { get; set; }
        public string? BankName { get; set; }
        public string? BankAccountNo { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? EmergencyContactRelation { get; set; }
        public string? EmployeeType { get; set; }
        public string? Status { get; set; } = "Active";
        public bool IsActive { get; set; } = true;
    }

    public class UpdateEmployeeDto
    {
        public string EmployeeCode { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; }
        public string? FirstNameUrdu { get; set; }
        public string? LastNameUrdu { get; set; }
        public string? CNIC { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime? JoiningDate { get; set; }
        public DateTime? LeavingDate { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Photo { get; set; }
        public string? Qualification { get; set; }
        public string? Specialization { get; set; }
        public string? Experience { get; set; }
        public string? Department { get; set; }
        public int? DesignationId { get; set; }
        public int? HRGradeId { get; set; }
        public int? AcademicGradeId { get; set; }
        public decimal? BasicSalary { get; set; }
        public decimal? Allowances { get; set; }
        public decimal? Deductions { get; set; }
        public decimal? NetSalary { get; set; }
        public string? BankName { get; set; }
        public string? BankAccountNo { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? EmergencyContactRelation { get; set; }
        public string? EmployeeType { get; set; }
        public string? Status { get; set; }
        public bool IsActive { get; set; }
    }

    public class EmployeeDropdownDto
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string DisplayText => $"{EmployeeCode} - {FullName}";
    }
}

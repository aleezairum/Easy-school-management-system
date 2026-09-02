namespace School.API.DTOs.HR
{
    public class EmployeeListDto
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? FatherName { get; set; }
        public string? CNIC { get; set; }
        public string? Mobile { get; set; }
        public string? Photo { get; set; }
        public string? Department { get; set; }
        public int? DesignationId { get; set; }
        public string? DesignationName { get; set; }
        public string? HRGradeName { get; set; }
        public string? EmployeeType { get; set; }
        public string? Status { get; set; }
        public DateTime? JoiningDate { get; set; }
        public bool IsTeachingStaff { get; set; }
        public bool IsActive { get; set; }
    }

    public class EmployeeDto
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; }
        public string? FirstNameUrdu { get; set; }
        public string? LastNameUrdu { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? FatherName { get; set; }
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

        // Payroll
        public int? ShiftId { get; set; }
        public string? ShiftName { get; set; }
        public string? MachineCardNo { get; set; }
        public byte? OffDay1 { get; set; }
        public byte? OffDay2 { get; set; }

        // Teaching staff are rows in this same table; there is no separate
        // academic record and no separate id to carry.
        public bool IsTeachingStaff { get; set; }

        public string? BankName { get; set; }
        public string? BankAccountNo { get; set; }

        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? EmergencyContactRelation { get; set; }

        public string? EmployeeType { get; set; }
        public string? Status { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }

    public class SaveEmployeeDto
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; }
        public string? FirstNameUrdu { get; set; }
        public string? LastNameUrdu { get; set; }
        public string? FatherName { get; set; }
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

        public int? ShiftId { get; set; }
        public string? MachineCardNo { get; set; }
        public byte? OffDay1 { get; set; }
        public byte? OffDay2 { get; set; }

        // Ticking this is the whole of "is a teacher": it makes the employee
        // available to subject assignment, the timetable and the exam reports,
        // all of which join this employee's own Id.
        public bool IsTeachingStaff { get; set; }

        public string? BankName { get; set; }
        public string? BankAccountNo { get; set; }

        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? EmergencyContactRelation { get; set; }

        public string? EmployeeType { get; set; }
        public string? Status { get; set; } = "Active";
        public bool IsActive { get; set; } = true;
    }

    public class EmployeeDropdownDto
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using School.API.Models;

namespace School.API.Data.DBModels.HR
{
    [Table("Employees")]
    public class Employee
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string EmployeeCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? LastName { get; set; }

        [MaxLength(100)]
        public string? FirstNameUrdu { get; set; }

        [MaxLength(100)]
        public string? LastNameUrdu { get; set; }

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}".Trim();

        [MaxLength(20)]
        public string? CNIC { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(20)]
        public string? Mobile { get; set; }

        [MaxLength(10)]
        public string? Gender { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public DateTime? JoiningDate { get; set; }

        public DateTime? LeavingDate { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(255)]
        public string? Photo { get; set; }

        [MaxLength(100)]
        public string? Qualification { get; set; }

        [MaxLength(100)]
        public string? Specialization { get; set; }

        [MaxLength(100)]
        public string? Experience { get; set; }

        [MaxLength(100)]
        public string? Department { get; set; }

        // Foreign Keys
        public int? DesignationId { get; set; }

        public int? HRGradeId { get; set; }

        public int? AcademicGradeId { get; set; }

        // Payroll (HR-Payroll-Final-Plan.md section 3)
        public int? ShiftId { get; set; }

        [MaxLength(50)]
        public string? MachineCardNo { get; set; }

        public byte? OffDay1 { get; set; }
        public byte? OffDay2 { get; set; }

        // Links to the academic SMSTeacher record for staff who also teach.
        // Payroll runs off Employees; academic reports keep running off
        // SMSTeacher untouched (D6 — see HR-Payroll-Final-Plan.md section 2).
        public int? TeacherId { get; set; }

        [MaxLength(100)]
        public string? BankName { get; set; }

        [MaxLength(50)]
        public string? BankAccountNo { get; set; }

        // Emergency Contact
        [MaxLength(100)]
        public string? EmergencyContactName { get; set; }

        [MaxLength(20)]
        public string? EmergencyContactPhone { get; set; }

        [MaxLength(50)]
        public string? EmergencyContactRelation { get; set; }

        [MaxLength(50)]
        public string? EmployeeType { get; set; } // Permanent, Contractual, Visiting

        [MaxLength(50)]
        public string? Status { get; set; } = "Active"; // Active, Inactive, Resigned, Terminated

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("DesignationId")]
        public virtual Designation? Designation { get; set; }

        [ForeignKey("HRGradeId")]
        public virtual HRGrade? HRGrade { get; set; }

        [ForeignKey("ShiftId")]
        public virtual HrShift? Shift { get; set; }

        [ForeignKey("TeacherId")]
        public virtual Teacher? Teacher { get; set; }

        // User navigation property for bidirectional relationship
        public virtual User? User { get; set; }
    }
}

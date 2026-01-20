using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Models
{
    [Table("Admissions")]
    public class Admission
    {
        [Key]
        public int Id { get; set; }

        // Admission Info
        public DateTime? DateOfAdmission { get; set; }

        [MaxLength(50)]
        public string? AdmissionNo { get; set; }

        [MaxLength(100)]
        public string? ClassSought { get; set; }

        // Student Info
        [Required]
        [MaxLength(200)]
        public string NameOfStudent { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? NameOfStudentUrdu { get; set; }

        // Father Info
        [MaxLength(200)]
        public string? FatherName { get; set; }

        [MaxLength(200)]
        public string? FatherNameUrdu { get; set; }

        [MaxLength(20)]
        public string? FatherCNIC { get; set; }

        [MaxLength(100)]
        public string? FatherOccupation { get; set; }

        [MaxLength(20)]
        public string? FatherMobile { get; set; }

        // Mother Info
        [MaxLength(200)]
        public string? MotherName { get; set; }

        [MaxLength(20)]
        public string? MotherCNIC { get; set; }

        [MaxLength(20)]
        public string? MotherMobile { get; set; }

        // Guardian Info
        [MaxLength(200)]
        public string? GuardianName { get; set; }

        [MaxLength(20)]
        public string? GuardianCNIC { get; set; }

        [MaxLength(100)]
        public string? GuardianRelation { get; set; }

        [MaxLength(20)]
        public string? GuardianMobile { get; set; }

        // Student Details
        public DateTime? DateOfBirth { get; set; }

        [MaxLength(200)]
        public string? DateOfBirthInWords { get; set; }

        [MaxLength(200)]
        public string? PlaceOfBirth { get; set; }

        [MaxLength(50)]
        public string? FormBNo { get; set; }

        [MaxLength(20)]
        public string? Gender { get; set; }

        [MaxLength(50)]
        public string? Religion { get; set; }

        // Student Photo
        [MaxLength(255)]
        public string? StudentPhoto { get; set; }

        // Address
        [MaxLength(500)]
        public string? PresentAddress { get; set; }

        [MaxLength(500)]
        public string? PresentAddressUrdu { get; set; }

        [MaxLength(500)]
        public string? PermanentAddress { get; set; }

        [MaxLength(500)]
        public string? PermanentAddressUrdu { get; set; }

        [MaxLength(20)]
        public string? PhoneResidence { get; set; }

        [MaxLength(20)]
        public string? EmergencyContact { get; set; }

        // Previous School
        [MaxLength(300)]
        public string? PreviousSchool { get; set; }

        [MaxLength(50)]
        public string? LastClass { get; set; }

        [MaxLength(100)]
        public string? Board { get; set; }

        [MaxLength(10)]
        public string? YearOfPassing { get; set; }

        [MaxLength(10)]
        public string? MarksObtained { get; set; }

        [MaxLength(10)]
        public string? TotalMarks { get; set; }

        [MaxLength(10)]
        public string? Percentage { get; set; }

        // For Office Use Only
        [MaxLength(50)]
        public string? RegistrationNo { get; set; }

        [MaxLength(50)]
        public string? RollNo { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? AdmissionFee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TuitionFee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? OtherCharges { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalFee { get; set; }

        [MaxLength(10)]
        public string? TestMarks { get; set; }

        [MaxLength(10)]
        public string? TestTotalMarks { get; set; }

        [MaxLength(10)]
        public string? TestPercentage { get; set; }

        [MaxLength(10)]
        public string? TestGrade { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        // Status
        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}

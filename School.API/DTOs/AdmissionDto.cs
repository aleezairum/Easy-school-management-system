namespace School.API.DTOs
{
    public class AdmissionDto
    {
        public int Id { get; set; }

        // Admission Info
        public DateTime? DateOfAdmission { get; set; }
        public string? AdmissionNo { get; set; }
        public string? ClassSought { get; set; }

        // Student Info
        public string NameOfStudent { get; set; } = string.Empty;
        public string? NameOfStudentUrdu { get; set; }

        // Father Info
        public string? FatherName { get; set; }
        public string? FatherNameUrdu { get; set; }
        public string? FatherCNIC { get; set; }
        public string? FatherOccupation { get; set; }
        public string? FatherMobile { get; set; }

        // Mother Info
        public string? MotherName { get; set; }
        public string? MotherCNIC { get; set; }
        public string? MotherMobile { get; set; }

        // Guardian Info
        public string? GuardianName { get; set; }
        public string? GuardianCNIC { get; set; }
        public string? GuardianRelation { get; set; }
        public string? GuardianMobile { get; set; }

        // Student Details
        public DateTime? DateOfBirth { get; set; }
        public string? DateOfBirthInWords { get; set; }
        public string? PlaceOfBirth { get; set; }
        public string? FormBNo { get; set; }
        public string? Gender { get; set; }
        public string? Religion { get; set; }

        // Student Photo
        public string? StudentPhoto { get; set; }

        // Address
        public string? PresentAddress { get; set; }
        public string? PresentAddressUrdu { get; set; }
        public string? PermanentAddress { get; set; }
        public string? PermanentAddressUrdu { get; set; }
        public string? PhoneResidence { get; set; }
        public string? EmergencyContact { get; set; }

        // Previous School
        public string? PreviousSchool { get; set; }
        public string? LastClass { get; set; }
        public string? Board { get; set; }
        public string? YearOfPassing { get; set; }
        public string? MarksObtained { get; set; }
        public string? TotalMarks { get; set; }
        public string? Percentage { get; set; }

        // For Office Use Only
        public string? RegistrationNo { get; set; }
        public string? RollNo { get; set; }
        public decimal? AdmissionFee { get; set; }
        public decimal? TuitionFee { get; set; }
        public decimal? OtherCharges { get; set; }
        public decimal? TotalFee { get; set; }
        public string? TestMarks { get; set; }
        public string? TestTotalMarks { get; set; }
        public string? TestPercentage { get; set; }
        public string? TestGrade { get; set; }
        public string? Remarks { get; set; }

        // Status
        public string Status { get; set; } = "Pending";
        public bool IsActive { get; set; }
    }

    public class AdmissionListDto
    {
        public int Id { get; set; }
        public string? AdmissionNo { get; set; }
        public string NameOfStudent { get; set; } = string.Empty;
        public string? FatherName { get; set; }
        public string? ClassSought { get; set; }
        public DateTime? DateOfAdmission { get; set; }
        public string? FatherMobile { get; set; }
        public string Status { get; set; } = "Pending";
        public bool IsActive { get; set; }
    }

    public class CreateAdmissionDto
    {
        // Admission Info
        public DateTime? DateOfAdmission { get; set; }
        public string? AdmissionNo { get; set; }
        public string? ClassSought { get; set; }

        // Student Info
        public string NameOfStudent { get; set; } = string.Empty;
        public string? NameOfStudentUrdu { get; set; }

        // Father Info
        public string? FatherName { get; set; }
        public string? FatherNameUrdu { get; set; }
        public string? FatherCNIC { get; set; }
        public string? FatherOccupation { get; set; }
        public string? FatherMobile { get; set; }

        // Mother Info
        public string? MotherName { get; set; }
        public string? MotherCNIC { get; set; }
        public string? MotherMobile { get; set; }

        // Guardian Info
        public string? GuardianName { get; set; }
        public string? GuardianCNIC { get; set; }
        public string? GuardianRelation { get; set; }
        public string? GuardianMobile { get; set; }

        // Student Details
        public DateTime? DateOfBirth { get; set; }
        public string? DateOfBirthInWords { get; set; }
        public string? PlaceOfBirth { get; set; }
        public string? FormBNo { get; set; }
        public string? Gender { get; set; }
        public string? Religion { get; set; }

        // Student Photo
        public string? StudentPhoto { get; set; }

        // Address
        public string? PresentAddress { get; set; }
        public string? PresentAddressUrdu { get; set; }
        public string? PermanentAddress { get; set; }
        public string? PermanentAddressUrdu { get; set; }
        public string? PhoneResidence { get; set; }
        public string? EmergencyContact { get; set; }

        // Previous School
        public string? PreviousSchool { get; set; }
        public string? LastClass { get; set; }
        public string? Board { get; set; }
        public string? YearOfPassing { get; set; }
        public string? MarksObtained { get; set; }
        public string? TotalMarks { get; set; }
        public string? Percentage { get; set; }

        // For Office Use Only
        public string? RegistrationNo { get; set; }
        public string? RollNo { get; set; }
        public decimal? AdmissionFee { get; set; }
        public decimal? TuitionFee { get; set; }
        public decimal? OtherCharges { get; set; }
        public decimal? TotalFee { get; set; }
        public string? TestMarks { get; set; }
        public string? TestTotalMarks { get; set; }
        public string? TestPercentage { get; set; }
        public string? TestGrade { get; set; }
        public string? Remarks { get; set; }
    }

    public class UpdateAdmissionDto
    {
        public int Id { get; set; }

        // Admission Info
        public DateTime? DateOfAdmission { get; set; }
        public string? AdmissionNo { get; set; }
        public string? ClassSought { get; set; }

        // Student Info
        public string NameOfStudent { get; set; } = string.Empty;
        public string? NameOfStudentUrdu { get; set; }

        // Father Info
        public string? FatherName { get; set; }
        public string? FatherNameUrdu { get; set; }
        public string? FatherCNIC { get; set; }
        public string? FatherOccupation { get; set; }
        public string? FatherMobile { get; set; }

        // Mother Info
        public string? MotherName { get; set; }
        public string? MotherCNIC { get; set; }
        public string? MotherMobile { get; set; }

        // Guardian Info
        public string? GuardianName { get; set; }
        public string? GuardianCNIC { get; set; }
        public string? GuardianRelation { get; set; }
        public string? GuardianMobile { get; set; }

        // Student Details
        public DateTime? DateOfBirth { get; set; }
        public string? DateOfBirthInWords { get; set; }
        public string? PlaceOfBirth { get; set; }
        public string? FormBNo { get; set; }
        public string? Gender { get; set; }
        public string? Religion { get; set; }

        // Student Photo
        public string? StudentPhoto { get; set; }

        // Address
        public string? PresentAddress { get; set; }
        public string? PresentAddressUrdu { get; set; }
        public string? PermanentAddress { get; set; }
        public string? PermanentAddressUrdu { get; set; }
        public string? PhoneResidence { get; set; }
        public string? EmergencyContact { get; set; }

        // Previous School
        public string? PreviousSchool { get; set; }
        public string? LastClass { get; set; }
        public string? Board { get; set; }
        public string? YearOfPassing { get; set; }
        public string? MarksObtained { get; set; }
        public string? TotalMarks { get; set; }
        public string? Percentage { get; set; }

        // For Office Use Only
        public string? RegistrationNo { get; set; }
        public string? RollNo { get; set; }
        public decimal? AdmissionFee { get; set; }
        public decimal? TuitionFee { get; set; }
        public decimal? OtherCharges { get; set; }
        public decimal? TotalFee { get; set; }
        public string? TestMarks { get; set; }
        public string? TestTotalMarks { get; set; }
        public string? TestPercentage { get; set; }
        public string? TestGrade { get; set; }
        public string? Remarks { get; set; }

        // Status
        public string Status { get; set; } = "Pending";
        public bool IsActive { get; set; }
    }

    public class UpdateAdmissionStatusDto
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty; // Pending, Approved, Rejected
    }
}

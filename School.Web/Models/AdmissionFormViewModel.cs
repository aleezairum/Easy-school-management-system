namespace School.Web.Models
{
    public class AdmissionFormViewModel
    {
        // Row 1
        public string? DateOfAdmission { get; set; }
        public string? AdmissionNo { get; set; }

        // Row 2
        public string? ClassSought { get; set; }

        // Student Name
        public string? NameOfStudent { get; set; }
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
        public string? DateOfBirth { get; set; }
        public string? DateOfBirthInWords { get; set; }
        public string? PlaceOfBirth { get; set; }
        public string? FormBNo { get; set; }
        public string? Gender { get; set; }
        public string? Religion { get; set; }

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
        public string? AdmissionFee { get; set; }
        public string? TuitionFee { get; set; }
        public string? OtherCharges { get; set; }
        public string? TotalFee { get; set; }
        public string? TestMarks { get; set; }
        public string? TestTotalMarks { get; set; }
        public string? TestPercentage { get; set; }
        public string? TestGrade { get; set; }
        public string? Remarks { get; set; }
    }
}

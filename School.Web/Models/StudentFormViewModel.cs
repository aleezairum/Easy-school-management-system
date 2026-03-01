using Microsoft.AspNetCore.Http;
using System;

namespace School.Web.Models
{
    public class StudentFormViewModel
    {
        public int Id { get; set; }

        // Link to Admission
        public int? AdmissionId { get; set; }

        // Student Registration Info
        public string? RegistrationNo { get; set; }
        public string? RollNo { get; set; }
        public string? AdmissionNo { get; set; }
        public string? DateOfAdmission { get; set; }

        // Current Class/Section Info
        public string? CurrentClass { get; set; }
        public string? Section { get; set; }

        // Student Info
        public string? NameOfStudent { get; set; }
        public string? NameOfStudentUrdu { get; set; }

        // Father Info
        public string? FatherName { get; set; }
        public string? FatherNameUrdu { get; set; }
        public string? FatherCNIC { get; set; }
        public string? FatherOccupation { get; set; }
        public string? FatherMobile { get; set; }
        public bool FatherIsSMS { get; set; }

        // Mother Info
        public string? MotherName { get; set; }
        public string? MotherNameUrdu { get; set; }
        public string? MotherCNIC { get; set; }
        public string? MotherMobile { get; set; }
        public bool MotherIsSMS { get; set; }

        // Guardian Info
        public int GuardianType { get; set; } = 2; // 0=Father, 1=Mother, 2=Other
        public string? GuardianName { get; set; }
        public string? GuardianNameUrdu { get; set; }
        public string? GuardianCNIC { get; set; }
        public string? GuardianRelation { get; set; }
        public string? GuardianMobile { get; set; }
        public bool GuardianIsSMS { get; set; }

        // Student Contact
        public string? MobileNo { get; set; }
        public bool IsSMS { get; set; }

        // Student Details
        public string? DateOfBirth { get; set; }
        public string? DateOfBirthInWords { get; set; }
        public string? PlaceOfBirth { get; set; }
        public string? FormBNo { get; set; }
        public string? Gender { get; set; }
        public string? Religion { get; set; }
        public string? BloodGroup { get; set; }
        public string? Nationality { get; set; }

        // Student Photo
        public string? StudentPhoto { get; set; }
        public IFormFile? PhotoFile { get; set; }

        // Address
        public string? PresentAddress { get; set; }
        public string? PresentAddressUrdu { get; set; }
        public string? PermanentAddress { get; set; }
        public string? PermanentAddressUrdu { get; set; }
        public string? PhoneResidence { get; set; }
        public string? EmergencyContact { get; set; }
        public string? Email { get; set; }

        // Previous School
        public string? PreviousSchool { get; set; }
        public string? LastClass { get; set; }
        public string? Board { get; set; }
        public string? YearOfPassing { get; set; }
        public string? MarksObtained { get; set; }
        public string? TotalMarks { get; set; }
        public string? Percentage { get; set; }

        // Fee Info
        public string? MonthlyFee { get; set; }
        public string? AdmissionFee { get; set; }
        public string? FeeCategory { get; set; }

        // Status
        public int? StatusID { get; set; }
        public string? Status { get; set; }
        public bool IsActive { get; set; } = true;

        // API deserialization aliases (API returns different field names)
        public int VID { get => Id; set => Id = value; }
        public string? Name { get => NameOfStudent; set => NameOfStudent = value; }
        public string? Name_Urdu { get => NameOfStudentUrdu; set => NameOfStudentUrdu = value; }
        public string? Occupation { get => FatherOccupation; set => FatherOccupation = value; }
        public string? BirthPlace { get => PlaceOfBirth; set => PlaceOfBirth = value; }
        public string? ClassName { get => CurrentClass; set => CurrentClass = value; }
        public string? SectionName { get => Section; set => Section = value; }
        public string? Address { get => PresentAddress; set => PresentAddress = value; }
        public bool? IsFatherSMS { get => FatherIsSMS; set => FatherIsSMS = value ?? false; }
        public bool? IsMotherSMS { get => MotherIsSMS; set => MotherIsSMS = value ?? false; }
        public bool? IsGuardianSMS { get => GuardianIsSMS; set => GuardianIsSMS = value ?? false; }
        public DateTime? DOB
        {
            get => string.IsNullOrEmpty(DateOfBirth) ? null : DateTime.Parse(DateOfBirth);
            set => DateOfBirth = value?.ToString("yyyy-MM-dd");
        }
        public DateTime? AdmissionDate
        {
            get => string.IsNullOrEmpty(DateOfAdmission) ? null : DateTime.Parse(DateOfAdmission);
            set => DateOfAdmission = value?.ToString("yyyy-MM-dd");
        }
    }

    public class StudentListViewModel
    {
        public int Id { get; set; }
        public string? RegistrationNo { get; set; }
        public string? RollNo { get; set; }
        public string? NameOfStudent { get; set; }
        public string? FatherName { get; set; }
        public string? CurrentClass { get; set; }
        public string? Section { get; set; }
        public string? FatherMobile { get; set; }
        public string? StudentPhoto { get; set; }
        public string? Status { get; set; }
        public bool IsActive { get; set; }
    }

    public class AdmissionDropdownItem
    {
        public int Id { get; set; }
        public string? AdmissionNo { get; set; }
        public string? NameOfStudent { get; set; }
        public string? FatherName { get; set; }
        public string? ClassSought { get; set; }
        public string? DateOfAdmission { get; set; }
        public string? Status { get; set; }

        public string DisplayText => $"{AdmissionNo} - {NameOfStudent} ({FatherName}) - {ClassSought}";
    }
}

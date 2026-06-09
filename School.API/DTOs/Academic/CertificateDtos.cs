namespace School.API.DTOs.Academic
{
    // Output of SpGet_SMSStudentLeavingCertificate
    public class LeavingCertificateDto
    {
        public int VID { get; set; }
        public int? CampusID { get; set; }
        public string? AdmissionNo { get; set; }
        public DateTime? AdmissionDate { get; set; }
        public string? RollNo { get; set; }
        public string? StudentName { get; set; }
        public string? Name_Urdu { get; set; }
        public DateTime? DOB { get; set; }
        public string? Gender { get; set; }
        public string? Address { get; set; }
        public string? FatherName { get; set; }
        public string? FatherNameUrdu { get; set; }
        public string? FatherCNIC { get; set; }
        public string? FatherMobile { get; set; }
        public string? ClassAdmitted { get; set; }
        public string? ClassStudying { get; set; }
        public DateTime? LeavingDate { get; set; }
        public string? Examination { get; set; }
        public string? MarksObtained { get; set; }
        public string? TotalMarks { get; set; }
    }

    // Output of SpGet_SMSStudentProvisionalCertificate
    public class ProvisionalCertificateDto
    {
        public int VID { get; set; }
        public int? CampusID { get; set; }
        public string? RegistrationNo { get; set; }
        public string? AdmissionNo { get; set; }
        public DateTime? AdmissionDate { get; set; }
        public string? RollNo { get; set; }
        public string? StudentName { get; set; }
        public string? Name_Urdu { get; set; }
        public DateTime? DOB { get; set; }
        public string? Gender { get; set; }
        public string? Address { get; set; }
        public string? FatherName { get; set; }
        public string? FatherNameUrdu { get; set; }
        public string? FatherCNIC { get; set; }
        public string? SessionName { get; set; }
        public string? BoardRollNo { get; set; }
        public string? ExamRollNo { get; set; }
        public string? Examination { get; set; }
        public string? MarksObtained { get; set; }
        public string? TotalMarks { get; set; }
        public string? Conduct { get; set; }
        public string? MoralCharacter { get; set; }
        public DateTime? IssueDate { get; set; }
    }
}

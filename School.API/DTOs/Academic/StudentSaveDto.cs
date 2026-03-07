namespace School.API.DTOs.Academic
{
    public class StudentSaveDto
    {
        public int VID { get; set; }
        public int ParentID { get; set; }

        public string RegistrationNo { get; set; }

        public string AdmissionNo { get; set; }

        public DateTime? AdmissionDate { get; set; }

        public string RollNo { get; set; }

        public string Name { get; set; }

        public string Name_Urdu { get; set; }

        public string MobileNo { get; set; }

        public bool? IsSMS { get; set; }

        public string Gender { get; set; }

        public DateTime? DOB { get; set; }

        public string BirthPlace { get; set; }

        public string FormBNo { get; set; }

        public string Religion { get; set; }

        public string Caste { get; set; }

        public string Address { get; set; }

        public string FatherName { get; set; }

        public string FatherNameUrdu { get; set; }

        public string FatherCNIC { get; set; }

        public string FatherMobile { get; set; }

        public bool? IsFatherSMS { get; set; }

        public string Occupation { get; set; }

        public string MotherName { get; set; }

        public string MotherNameUrdu { get; set; }

        public string MotherCNIC { get; set; }

        public string MotherMobile { get; set; }

        public bool? IsMotherSMS { get; set; }

        public int GuardianType { get; set; }
        public string GuardianName { get; set; }

        public string GuardianNameUrdu { get; set; }

        public string GuardianCNIC { get; set; }

        public string GuardianMobile { get; set; }

        public bool? IsGuardianSMS { get; set; }

        public string GuardianRelation { get; set; }

        public int SessionID { get; set; }

        public int SectionID { get; set; }

        public string ClassRollNo { get; set; }
        public decimal Fee { get; set; }
        public string GradeName { get; set; }

        public int? GradeID { get; set; }

        public int? StatusID { get; set; }

        public bool? IsAvailAcademy { get; set; }

        public string SectionName { get; set; }

        public int ClassID { get; set; }

        public string ClassName { get; set; }

        public string SessionName { get; set; }

        public bool IsActive { get; set; } = true;

        public int? InsertedBy { get; set; }
        public DateTime? InsertedDate { get; set; }
        public string? InsertedIp { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedIp { get; set; }
    }

    public class StudentComboDto
    {
        public int VID { get; set; }
        public string RollNo { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Name_Urdu { get; set; }
        public string? ClassRollNo { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace School.API.Data.DBModels.Accounts
{
    public class Student
    {
        [Key]
        public int VID { get; set; }
        public string RegistrationNo { get; set; }

        public string AdmissionNo { get; set; }

        public DateTime? AdmissionDate { get; set; }

        public string Name { get; set; }

        public string Name_Urdu { get; set; }

        public string Gender { get; set; }

        public DateTime? DOB { get; set; }

        public string BirthPlace { get; set; }

        public string FormBNo { get; set; }

        public string Religion { get; set; }

        public string Caste { get; set; }

        public string Address { get; set; }

        public bool? StudentActive { get; set; }

        public int ParentID { get; set; }

        public string FatherName { get; set; }

        public string FatherCNIC { get; set; }

        public string FatherMobile { get; set; }

        public string Occupation { get; set; }

        public string MotherName { get; set; }

        public string MotherMobile { get; set; }

        public string GuardianName { get; set; }

        public string GuardianCNIC { get; set; }

        public string GuardianMobile { get; set; }

        public string GuardianRelation { get; set; }

        public bool? ParentActive { get; set; }

        public int AcademicID { get; set; }

        public int SessionID { get; set; }

        public int SectionID { get; set; }

        public string RollNo { get; set; }

        public int? GradeID { get; set; }

        public bool? AcademicActive { get; set; }
        public bool IsActive { get; set; } = true;

        public int? InsertedBy { get; set; }
        public DateTime InsertedDate { get; set; } = DateTime.UtcNow;
        public string? InsertedIp { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedIp { get; set; }
    }
}

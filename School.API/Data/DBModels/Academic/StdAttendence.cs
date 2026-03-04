using System.ComponentModel.DataAnnotations;

namespace School.API.Data.DBModels.Student
{
    public class StdAttendence
    {
        [Key]
        public int VID { get; set; }
        public int SessionID { get; set; }
        public string? SessionName { get; set; }
        public int StudentID { get; set; }
        public string? StudentName { get; set; }
        public int SectionID { get; set; }
        public string? SectionName { get; set; }
        public DateTime VDate { get; set; }
        public int Attendence { get; set; }
        public bool IsLate { get; set; }
        public bool IsAgain { get; set; }
        public string? Remarks { get; set; }
        public bool ISentSMS { get; set; }
        public int? InsertedBy { get; set; }
        public DateTime? InsertedDate { get; set; }
        public string? InsertedIp { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedIp { get; set; }
    }
}
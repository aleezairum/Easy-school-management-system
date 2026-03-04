namespace School.API.DTOs.Student
{
    public class StdAttendenceSaveDto
    {
        public int VID { get; set; }
        public int SessionID { get; set; }
        public int StudentID { get; set; }
        public int SectionID { get; set; }
        public DateTime VDate { get; set; }
        public int Attendence { get; set; }
        public bool IsLate { get; set; }
        public bool IsAgain { get; set; }
        public string? Remarks { get; set; }
        public bool ISentSMS { get; set; }
    }

    public class StdAttendenceFilterDto
    {
        public int? VID { get; set; }
        public int? SessionID { get; set; }
        public int? StudentID { get; set; }
        public int? SectionID { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}
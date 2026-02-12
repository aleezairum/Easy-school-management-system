namespace School.API.DTOs.Accounts
{
    public class FeeStructureSaveDto
    {
        public int VID { get; set; }
        public int? AcademicSessionID { get; set; }
        public int? ClassID { get; set; }
        public int? GradeID { get; set; }
        public int? FeeTypeID { get; set; }
        public decimal? Amount { get; set; }
        public bool? IsActive { get; set; }

        // Display names returned by SP joins
        public string? AcademicSessionName { get; set; }
        public string? ClassName { get; set; }
        public string? GradeName { get; set; }
        public string? FeeTypeName { get; set; }

        public int? InsertedBy { get; set; }
        public DateTime? InsertedDate { get; set; }
        public string? InsertedIp { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedIp { get; set; }
    }
}

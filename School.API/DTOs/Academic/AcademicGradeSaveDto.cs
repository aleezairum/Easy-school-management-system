namespace School.API.DTOs
{
    public class AcademicGradeSaveDto
    {
        public int VID { get; set; }

        public string VName { get; set; }

        public decimal MinPercentage { get; set; }

        public decimal MaxPercentage { get; set; }

        public decimal? GradePoint { get; set; }

        public string Remarks { get; set; }

        public bool IsActive { get; set; }

        public int? InsertedBy { get; set; }

        public DateTime? InsertedDate { get; set; }

        public string InsertedIp { get; set; }

        public int? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public string UpdatedIp { get; set; }

    }

}

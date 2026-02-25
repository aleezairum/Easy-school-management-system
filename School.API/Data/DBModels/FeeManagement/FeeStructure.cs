using System.ComponentModel.DataAnnotations;

namespace School.API.Data.DBModels.Accounts
{
    public class FeeStructure
    {
        [Key]
        public int VID { get; set; }

        public int CampusID { get; set; }
        public int AcademicSessionID { get; set; }
        public int ClassID { get; set; }
        public int GradeID { get; set; } = 0;
        public int FeeTypeID { get; set; }

        public decimal Amount { get; set; }

        public bool IsActive { get; set; } = true;

        public int? InsertedBy { get; set; }
        public DateTime InsertedDate { get; set; } = DateTime.UtcNow;
        public string? InsertedIp { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedIp { get; set; }
    }
}

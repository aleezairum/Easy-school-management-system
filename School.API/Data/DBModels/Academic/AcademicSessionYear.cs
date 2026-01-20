using System.ComponentModel.DataAnnotations;

namespace School.API.Data.DBModels.Academic
{
    public class AcademicSessionYear
    {
        [Key]
        public int VID { get; set; }
        public string VName { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public bool IsCurrent { get; set; }
        public bool IsActive { get; set; }
        public int? InsertedBy { get; set; }
        public DateTime? InsertedDate { get; set; }
        public string InsertedIp { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedIp { get; set; }
    }
}

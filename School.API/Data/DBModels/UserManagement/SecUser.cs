using System.ComponentModel.DataAnnotations;

namespace School.API.Data.DBModels.UserManagement
{
    public class SecUser
    {
        [Key]
        public int VID { get; set; }

        public int? EmployeeID { get; set; }

        [Required]
        [StringLength(150)]
        public string UserFullName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string UserLogin { get; set; } = string.Empty;

        public bool AllowAudit { get; set; } = false;
        public bool AllowActual { get; set; } = false;
        public bool IsAdmin { get; set; } = false;
        public int UserType { get; set; } = 0;
        public bool IsIndividual { get; set; } = false;
        public int DefaultCompanyID { get; set; } = 0;
        public int DefaultFinancialYearID { get; set; } = 0;
        public int UserSecFlage { get; set; } = 0;

        [StringLength(50)]
        public string? MacAddress { get; set; }

        [StringLength(100)]
        public string? ComputerName { get; set; }

        [StringLength(100)]
        public string? WindowLoginName { get; set; }

        public bool IsActive { get; set; } = true;

        public int? InsertedBy { get; set; }
        public DateTime InsertedDate { get; set; } = DateTime.UtcNow;
        public string? InsertedIp { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedIp { get; set; }
    }
}

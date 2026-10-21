using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Data.DBModels.Academic
{
    /// <summary>
    /// A campus — Boys or Girls.
    /// </summary>
    /// <remarks>
    /// The [Table] attribute is load-bearing. SchoolDbContext exposes this as
    /// DbSet&lt;SMSCampus&gt; Campuses, and EF names the table after the DbSet,
    /// so without it every EF query against campuses fails with
    /// "Invalid object name 'Campuses'". It went unnoticed because the campus
    /// screen reads through stored procedures; the first EF query to touch it
    /// was the employee list joining a campus name.
    /// </remarks>
    [Table("SMSCampus")]
    public class SMSCampus
    {
        [Key]
        public int VID { get; set; }

        [Required]
        [StringLength(100)]
        public string VName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public int? InsertedBy { get; set; }
        public DateTime InsertedDate { get; set; } = DateTime.UtcNow;
        public string? InsertedIp { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedIp { get; set; }

    }
}

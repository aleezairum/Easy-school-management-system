using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Data.DBModels.HR
{
    /// <summary>
    /// A group the office counts and prints staff by — Teaching, Non-Teaching,
    /// Admin, Support, or whatever the school decides.
    /// </summary>
    /// <remarks>
    /// Not the same thing as a designation, which is the job itself: Principal,
    /// Teacher, Coordinator, Office Incharge. Several designations fall into one
    /// category, and the two lists change for different reasons, so they are
    /// kept apart. See Scripts/StaffCategory.sql.
    /// </remarks>
    [Table("HrStaffCategory")]
    public class HrStaffCategory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Description { get; set; }

        public int? SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}

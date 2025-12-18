using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Models
{
    [Table("RolePermissions")]
    public class RolePermission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RoleId { get; set; }

        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; } = null!;

        [Required]
        public int MenuId { get; set; }

        [ForeignKey("MenuId")]
        public virtual Menu Menu { get; set; } = null!;

        public bool IsView { get; set; } = false;

        public bool IsInsert { get; set; } = false;

        public bool IsUpdate { get; set; } = false;

        public bool IsDelete { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public bool IsBackDate { get; set; } = false;

        public bool IsPrint { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}

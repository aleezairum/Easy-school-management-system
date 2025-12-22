using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Models
{
    [Table("Roles")]
    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string RoleName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}

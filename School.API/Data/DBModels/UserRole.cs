using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Models
{
    [Table("UserRoles")]
    public class UserRole
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [Required]
        public int RoleId { get; set; }

        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

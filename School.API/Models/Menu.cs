using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Models
{
    [Table("Menus")]
    public class Menu
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string MenuTitle { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Url { get; set; }

        [MaxLength(50)]
        public string? Icon { get; set; }

        public int? ParentId { get; set; }

        [ForeignKey("ParentId")]
        public virtual Menu? Parent { get; set; }

        public virtual ICollection<Menu> Children { get; set; } = new List<Menu>();

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public int Level { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}

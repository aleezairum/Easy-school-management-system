using System.ComponentModel.DataAnnotations;

namespace School.API.Models
{
    public class Session
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string SessionName { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public bool IsCurrent { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}

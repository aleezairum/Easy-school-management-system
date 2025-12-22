using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Models
{
    [Table("Employees")]
    public class Employee
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string EmployeeCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? LastName { get; set; }

        [MaxLength(200)]
        public string FullName => $"{FirstName} {LastName}".Trim();

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? Department { get; set; }

        [MaxLength(100)]
        public string? Designation { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public virtual User? User { get; set; }
    }
}

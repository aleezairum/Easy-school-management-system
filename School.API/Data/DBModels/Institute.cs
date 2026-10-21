using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Models
{
    /// <summary>
    /// The school's own particulars, as entered on the School Profile form.
    /// One row: this is a single school, not a chain.
    /// </summary>
    [Table("Institutes")]
    public class Institute
    {
        [Key]
        public int Id { get; set; }

        /// <summary>The full name as it is printed on documents.</summary>
        [Required]
        [MaxLength(400)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Short code. Filled in from the name when the form leaves it blank.</summary>
        [Required]
        [MaxLength(100)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(400)]
        public string? Tagline { get; set; }

        [MaxLength(100)]
        public string? RegistrationNo { get; set; }

        public int? EstablishedYear { get; set; }

        [MaxLength(1000)]
        public string? Address { get; set; }

        [MaxLength(200)]
        public string? City { get; set; }

        [MaxLength(200)]
        public string? State { get; set; }

        [MaxLength(200)]
        public string? Country { get; set; }

        [MaxLength(40)]
        public string? PostalCode { get; set; }

        [MaxLength(40)]
        public string? Phone { get; set; }

        [MaxLength(400)]
        public string? Email { get; set; }

        [MaxLength(400)]
        public string? Website { get; set; }

        [MaxLength(300)]
        public string? PrincipalName { get; set; }

        [MaxLength(40)]
        public string? PrincipalPhone { get; set; }

        [MaxLength(400)]
        public string? PrincipalEmail { get; set; }

        /// <summary>
        /// The logo, held inline as a data URL rather than as a path. A path
        /// would point at a file that has to survive every redeploy and be
        /// present on whichever machine renders the page; the row travels with
        /// the backup instead. NVARCHAR(MAX) — no length limit here.
        /// </summary>
        public string? LogoUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}

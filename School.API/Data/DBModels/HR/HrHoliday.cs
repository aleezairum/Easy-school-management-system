using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Data.DBModels.HR
{
    [Table("HrHoliday")]
    public class HrHoliday
    {
        [Key]
        public int Id { get; set; }

        [Column("HDate")]
        public DateTime HDate { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public HrHolidayType HType { get; set; }

        public bool IsActive { get; set; } = true;
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Data.DBModels.HR
{
    [Table("HrLeave")]
    public class HrLeave
    {
        [Key]
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        public int LeaveTypeId { get; set; }

        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal Days { get; set; }

        public HrLeaveStatus Status { get; set; } = HrLeaveStatus.Pending;

        [MaxLength(300)]
        public string? Reason { get; set; }

        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

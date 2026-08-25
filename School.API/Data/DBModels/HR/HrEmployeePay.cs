using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School.API.Data.DBModels.HR
{
    // One fixed-amount component line on an employee's salary structure.
    // Owned by HrEmployeePay, stored as JSON in ComponentsJson (D3).
    public class HrPayComponentAmount
    {
        public int ComponentId { get; set; }
        public decimal Amount { get; set; }
    }

    [Table("HrEmployeePay")]
    public class HrEmployeePay
    {
        [Key]
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public DateTime EffectiveFrom { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Basic { get; set; }

        // Materialized from/to ComponentsJson by EF (owned, ToJson).
        public List<HrPayComponentAmount> Components { get; set; } = new();

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

// DTOs/HR/StaffStatementDtos.cs
namespace School.API.DTOs.HR
{
    /// <summary>
    /// One line of the Staff Statement — the printed sheet the school submits,
    /// listing every employee with their qualification, appointment date and
    /// monthly salary.
    /// </summary>
    /// <remarks>
    /// Ordered by appointment date, oldest first, which is how the school's own
    /// sheet reads: the statement doubles as a seniority list.
    /// </remarks>
    public class StaffStatementRowDto
    {
        public int EmployeeId { get; set; }
        public string? EmployeeCode { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Designation { get; set; }
        public string? FatherName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Qualification { get; set; }
        public DateTime? JoiningDate { get; set; }

        /// <summary>
        /// Basic plus the fixed EARNING components on the pay structure in force
        /// on the report date. Null when the employee has no pay structure yet —
        /// printed blank rather than as a zero, because zero is a claim and blank
        /// is the truth.
        /// </summary>
        public decimal? MonthlySalary { get; set; }

        public string? ContactNo { get; set; }
        public bool IsActive { get; set; }
    }
}

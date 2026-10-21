using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using School.API.Models;

namespace School.API.Data.DBModels.HR
{
    /// <summary>
    /// The one staff record. HR and academics both hang off it.
    /// </summary>
    /// <remarks>
    /// This table IS the old SMSTeacher, renamed with its keys intact
    /// (RenameTeacherToEmployee.sql) — so <see cref="Id"/> is the value that
    /// SMSStaffSubject.TeacherID, SMSClassTimetable.TeacherID and every
    /// SpRPT_SMSExam* report already join on. There is no second teacher row to
    /// keep in sync any more; <see cref="IsTeachingStaff"/> is the only thing
    /// separating a teacher from any other employee.
    ///
    /// The database also carries a persisted computed column, VName, equal to
    /// "FirstName LastName". It is deliberately not mapped here — it exists so
    /// the legacy stored procedures can keep selecting the name they always
    /// selected, and it is derived, so nothing writes to it.
    /// </remarks>
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

        [MaxLength(100)]
        public string? FirstNameUrdu { get; set; }

        [MaxLength(100)]
        public string? LastNameUrdu { get; set; }

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}".Trim();

        [MaxLength(200)]
        public string? FatherName { get; set; }

        [MaxLength(20)]
        public string? CNIC { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(20)]
        public string? Mobile { get; set; }

        [MaxLength(10)]
        public string? Gender { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public DateTime? JoiningDate { get; set; }

        public DateTime? LeavingDate { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        // The picture itself as a data: URL (NVARCHAR(MAX), Scripts/HikvisionBiometrics.sql).
        public string? Photo { get; set; }

        // 500, not 100: the column came over from SMSTeacher, where qualifications
        // were free text and several rows use the length.
        [MaxLength(500)]
        public string? Qualification { get; set; }

        [MaxLength(100)]
        public string? Specialization { get; set; }

        [MaxLength(100)]
        public string? Experience { get; set; }

        [MaxLength(100)]
        public string? Department { get; set; }

        /// <summary>
        /// The campus this member of staff is posted to.
        /// </summary>
        /// <remarks>
        /// Nullable because the table already held staff before the column
        /// existed and there is no honest way to guess where each of them
        /// belongs. Null reads as "not recorded yet", and a campus filter
        /// simply does not match those rows until the office says which campus
        /// it is. See Scripts/AddCampusToEmployees.sql.
        /// </remarks>
        [Column("CampusID")]
        public int? CampusId { get; set; }

        // Foreign Keys
        public int? DesignationId { get; set; }

        public int? HRGradeId { get; set; }

        /// <summary>
        /// The group the office counts and prints this person by - Teaching,
        /// Non-Teaching, Admin, whatever the school has set up. Nullable: staff
        /// already on file predate the column, and null reads as "not set yet"
        /// rather than as a guess. See Scripts/StaffCategory.sql.
        /// </summary>
        public int? StaffCategoryId { get; set; }

        public int? AcademicGradeId { get; set; }

        // Payroll (HR-Payroll-Final-Plan.md section 3)

        [MaxLength(50)]
        public string? MachineCardNo { get; set; }

        public byte? OffDay1 { get; set; }
        public byte? OffDay2 { get; set; }

        // Makes this person available to subject assignment, the timetable and
        // the exam reports. Nothing else distinguishes teaching staff — they are
        // rows in this same table, addressed by this same Id.
        public bool IsTeachingStaff { get; set; }

        [MaxLength(100)]
        public string? BankName { get; set; }

        [MaxLength(50)]
        public string? BankAccountNo { get; set; }

        // Emergency Contact
        [MaxLength(100)]
        public string? EmergencyContactName { get; set; }

        [MaxLength(20)]
        public string? EmergencyContactPhone { get; set; }

        [MaxLength(50)]
        public string? EmergencyContactRelation { get; set; }

        [MaxLength(50)]
        public string? EmployeeType { get; set; } // Permanent, Contractual, Visiting

        [MaxLength(50)]
        public string? Status { get; set; } = "Active"; // Active, Inactive, Resigned, Terminated

        public bool IsActive { get; set; } = true;

        // Audit — the InsertedBy/InsertedDate/InsertedIp set the teacher screen
        // maintained, under the names the rest of the HR model uses.
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedBy { get; set; }

        [MaxLength(200)]
        public string? CreatedIp { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public int? UpdatedBy { get; set; }

        [MaxLength(200)]
        public string? UpdatedIp { get; set; }

        // Navigation properties
        [ForeignKey("DesignationId")]
        public virtual Designation? Designation { get; set; }

        [ForeignKey("HRGradeId")]
        public virtual HRGrade? HRGrade { get; set; }


        // User navigation property for bidirectional relationship
        public virtual User? User { get; set; }
    }
}

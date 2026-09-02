namespace School.API.DTOs
{
    /// <summary>
    /// The old SMSTeacher row shape, kept as a read-only compatibility contract
    /// for <c>GET api/teacher</c>.
    /// </summary>
    /// <remarks>
    /// There is no teacher table any more — SMSTeacher was renamed into
    /// Employees, so VID here is simply Employee.Id and VName is
    /// "FirstName LastName". Screens that already speak this shape (Exam
    /// Reports, Staff Subjects) keep working unchanged; anything new should
    /// read <c>api/employee/dropdown</c> instead.
    ///
    /// Write operations are gone with the table: staff are created and edited
    /// on the Employee screen, and ticking "Teaching staff" is what puts
    /// someone in this list.
    /// </remarks>
    public class TeacherDto
    {
        public int VID { get; set; }
        public string? VName { get; set; }
        public string? FatherName { get; set; }
        public string? CNIC { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Qualification { get; set; }
        public bool IsActive { get; set; }
    }
}

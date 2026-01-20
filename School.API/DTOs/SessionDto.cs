namespace School.API.DTOs
{
    public class SessionDto
    {
        public int Id { get; set; }
        public string SessionName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateSessionDto
    {
        public string SessionName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateSessionDto
    {
        public int Id { get; set; }
        public string SessionName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public bool IsActive { get; set; }
    }
}

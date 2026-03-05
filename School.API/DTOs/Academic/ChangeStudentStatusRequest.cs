namespace School.API.DTOs.Academic
{
    public class ChangeStudentStatusRequest
    {
        public string StudentIds { get; set; }
        public int StatusID { get; set; }
        public decimal Fee { get; set; }
        public string Description { get; set; }
    }
}

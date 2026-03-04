namespace School.API.DTOs.SMS
{
    public class SmsStudentSpDto
    {
        public int VID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? MobileNo { get; set; }
        public bool IsSMS { get; set; }
        public string? FatherMobile { get; set; }
        public bool IsFatherSMS { get; set; }
        public string? MotherMobile { get; set; }
        public bool IsMotherSMS { get; set; }
        public string? GuardianMobile { get; set; }
        public bool IsGuardianSMS { get; set; }
    }
}
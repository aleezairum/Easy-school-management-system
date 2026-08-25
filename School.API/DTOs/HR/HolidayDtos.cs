namespace School.API.DTOs.HR
{
    public class HolidayDto
    {
        public int Id { get; set; }
        public DateTime HDate { get; set; }
        public string Name { get; set; } = string.Empty;
        public byte HType { get; set; }
        public bool IsActive { get; set; }
    }

    public class SaveHolidayDto
    {
        public int Id { get; set; }
        public DateTime HDate { get; set; }
        public string Name { get; set; } = string.Empty;
        public byte HType { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class CopyHolidayYearDto
    {
        public int FromYear { get; set; }
        public int ToYear { get; set; }
    }
}

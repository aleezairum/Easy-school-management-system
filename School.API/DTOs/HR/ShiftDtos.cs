namespace School.API.DTOs.HR
{
    public class ShiftDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public TimeSpan TimeIn { get; set; }
        public TimeSpan TimeOut { get; set; }
        public int GraceMinutes { get; set; }
        public decimal WorkHours { get; set; }
        public int HalfDayMinutes { get; set; }
        public int FullDayMinutes { get; set; }
        public byte? OffDay1 { get; set; }
        public byte? OffDay2 { get; set; }
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool IsActive { get; set; }
    }

    public class SaveShiftDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public TimeSpan TimeIn { get; set; }
        public TimeSpan TimeOut { get; set; }
        public int GraceMinutes { get; set; }
        public decimal WorkHours { get; set; }
        public int HalfDayMinutes { get; set; }
        public int FullDayMinutes { get; set; }
        public byte? OffDay1 { get; set; }
        public byte? OffDay2 { get; set; }
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ShiftDropdownDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}

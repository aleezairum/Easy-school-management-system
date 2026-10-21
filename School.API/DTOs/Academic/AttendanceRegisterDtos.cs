namespace School.API.DTOs.Student
{
    /// <summary>One section's monthly attendance register: a row per student, a column per day.</summary>
    public class AttendanceRegisterDto
    {
        public int SectionId { get; set; }
        public string? SectionName { get; set; }
        public string? ClassName { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }

        /// <summary>Days the register was taken for this section (any student marked).</summary>
        public int SchoolDays { get; set; }

        public List<RegisterDayDto> Days { get; set; } = new();
        public List<RegisterStudentDto> Students { get; set; } = new();
    }

    public class RegisterDayDto
    {
        public int Day { get; set; }

        /// <summary>Mon, Tue ...</summary>
        public string Weekday { get; set; } = string.Empty;

        /// <summary>school (register taken) | sunday | holiday | none (not taken)</summary>
        public string Kind { get; set; } = "none";
        public string? Holiday { get; set; }
    }

    public class RegisterStudentDto
    {
        public int StudentId { get; set; }
        public string? RollNo { get; set; }
        public string? Name { get; set; }
        public string? FatherName { get; set; }

        /// <summary>No longer studying (left, passed out ...) but marked in this section this month.</summary>
        public bool Left { get; set; }

        /// <summary>One per day of the month: P present, T late, A absent, L leave, "" not marked.</summary>
        public List<string> Marks { get; set; } = new();

        public int Present { get; set; }   // late included
        public int Late { get; set; }
        public int Absent { get; set; }
        public int Leave { get; set; }

        /// <summary>Present (late included) out of the days the student was marked.</summary>
        public decimal? Percent { get; set; }
    }
}

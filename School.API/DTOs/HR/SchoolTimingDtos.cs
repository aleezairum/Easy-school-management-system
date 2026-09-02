namespace School.API.DTOs.HR
{
    public class SchoolTimingDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan TimeIn { get; set; }
        public TimeSpan TimeOut { get; set; }

        /// <summary>Fridays in this range run on these instead, when set.</summary>
        public TimeSpan? FridayTimeIn { get; set; }
        public TimeSpan? FridayTimeOut { get; set; }

        /// <summary>Weekly off, 1 = Sunday .. 7 = Saturday.</summary>
        public byte? OffDay1 { get; set; }
        public byte? OffDay2 { get; set; }

        public bool IsActive { get; set; }

        /// <summary>How many days the range actually resolved to (holidays excluded).</summary>
        public int DaysApplied { get; set; }

        /// <summary>Of those, how many carry times of their own rather than the range's.</summary>
        public int DaysEdited { get; set; }

        /// <summary>
        /// Days inside this range that a LATER timing has since taken over. The
        /// row still says what it says; those dates just answer to something else.
        /// </summary>
        public int DaysOverridden { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class SaveSchoolTimingDto
    {
        public string? Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan TimeIn { get; set; }
        public TimeSpan TimeOut { get; set; }

        // Both or neither. Set them and Fridays in the range get these instead.
        public TimeSpan? FridayTimeIn { get; set; }
        public TimeSpan? FridayTimeOut { get; set; }

        // Weekly off, 1 = Sunday .. 7 = Saturday. Those weekdays are dropped from
        // the range entirely -- the school is shut, so there is nothing to apply.
        public byte? OffDay1 { get; set; }
        public byte? OffDay2 { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// The individual days, as shown and edited on the screen. The range and
        /// the Friday times are what BUILDS this list, but once the user has each
        /// day in front of them, this list is the truth — send it and it is saved
        /// exactly as given.
        /// </summary>
        /// <remarks>
        /// Leave it empty and the range is expanded server-side instead, which is
        /// the older behaviour and still what an API caller without a screen wants.
        /// </remarks>
        public List<SaveSchoolTimingDayDto> Days { get; set; } = new();
    }

    /// <summary>One resolved school day — what attendance actually reads.</summary>
    public class SchoolTimingDayDto
    {
        public int Id { get; set; }
        public DateTime TDate { get; set; }

        /// <summary>1 = Sunday .. 7 = Saturday, the convention the rest of HR uses.</summary>
        public byte DayOfWeek { get; set; }

        public TimeSpan TimeIn { get; set; }
        public TimeSpan TimeOut { get; set; }

        /// <summary>Carries times of its own rather than the range's.</summary>
        public bool IsOverridden { get; set; }
    }

    /// <summary>One day as posted from the screen.</summary>
    public class SaveSchoolTimingDayDto
    {
        public DateTime TDate { get; set; }
        public TimeSpan TimeIn { get; set; }
        public TimeSpan TimeOut { get; set; }
    }

    /// <summary>Change one single day that is already saved.</summary>
    public class SaveSchoolTimingDayTimeDto
    {
        public TimeSpan TimeIn { get; set; }
        public TimeSpan TimeOut { get; set; }
    }

    /// <summary>One date, the times it will get, and whether those differ from the range's.</summary>
    public class SchoolTimingDayPlan
    {
        public DateTime Date { get; set; }
        public TimeSpan TimeIn { get; set; }
        public TimeSpan TimeOut { get; set; }
        public bool IsOverridden { get; set; }
    }
}

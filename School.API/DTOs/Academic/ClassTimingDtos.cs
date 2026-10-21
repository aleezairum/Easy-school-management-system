// DTOs/Academic/ClassTimingDtos.cs
namespace School.API.DTOs.Academic
{
    /// <summary>
    /// One class's opening and closing time on one campus.
    /// </summary>
    /// <remarks>
    /// Times are plain "HH:mm" strings, not TimeSpan, on purpose. SpGet_SMSClassTiming
    /// already formats them that way, the screen feeds them straight into
    /// &lt;input type="time"&gt; which speaks exactly that, and a clock time that
    /// never becomes a date cannot pick up a timezone on the way to the browser.
    ///
    /// A VID of 0 means this class has no timing saved yet — the editing grid
    /// lists every class, including the blank ones, because filling those in is
    /// the whole point of the screen.
    /// </remarks>
    public class ClassTimingDto
    {
        public int VID { get; set; }
        public int CampusID { get; set; }
        public string? CampusName { get; set; }
        public int ClassID { get; set; }
        public string? ClassName { get; set; }

        /// <summary>
        /// From SMSClass.VName_Urdu, for the parents' Urdu notice. Null when
        /// the class has no Urdu name yet — the notice falls back to its own
        /// wording and then to <see cref="ClassName"/>.
        /// </summary>
        public string? ClassNameUrdu { get; set; }

        public string? TimeIn { get; set; }
        public string? TimeOut { get; set; }

        /// <summary>Both set, or both blank — Friday is all-or-nothing.</summary>
        public string? FridayTimeIn { get; set; }
        public string? FridayTimeOut { get; set; }

        /// <summary>
        /// The afternoon academy session. Both blank means this class has no
        /// academy, and the printed notice leaves it out of that table rather
        /// than showing an empty row.
        /// </summary>
        public string? AcademyTimeIn { get; set; }
        public string? AcademyTimeOut { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>This class runs an academy session.</summary>
        public bool HasAcademy =>
            !string.IsNullOrWhiteSpace(AcademyTimeIn) && !string.IsNullOrWhiteSpace(AcademyTimeOut);

        /// <summary>Nothing entered for this class yet.</summary>
        public bool IsEmpty =>
            string.IsNullOrWhiteSpace(TimeIn) && string.IsNullOrWhiteSpace(TimeOut);
    }

    public class SaveClassTimingDto
    {
        public int VID { get; set; }
        public int CampusID { get; set; }
        public int ClassID { get; set; }

        public string? TimeIn { get; set; }
        public string? TimeOut { get; set; }
        public string? FridayTimeIn { get; set; }
        public string? FridayTimeOut { get; set; }
        public string? AcademyTimeIn { get; set; }
        public string? AcademyTimeOut { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// The whole grid in one post — every class on a campus at once.
    /// </summary>
    /// <remarks>
    /// The screen is a table the office fills in top to bottom, so saving row by
    /// row would mean a dozen requests and a half-saved campus if one of them
    /// failed. Rows left completely blank are skipped rather than rejected: a
    /// school does not have to give every class a timing.
    /// </remarks>
    public class SaveClassTimingBatchDto
    {
        public int CampusID { get; set; }
        public List<SaveClassTimingDto> Rows { get; set; } = new();
    }

    public class SaveClassTimingResultDto
    {
        public int TotalRequested { get; set; }
        public int TotalSaved { get; set; }
        public int TotalSkipped { get; set; }
        public int TotalFailed { get; set; }

        /// <summary>One line per row that failed, naming the class.</summary>
        public List<string> Errors { get; set; } = new();
    }
}

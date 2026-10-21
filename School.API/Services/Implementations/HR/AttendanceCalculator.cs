using School.API.Data.DBModels.HR;

namespace School.API.Services.Implementations.HR
{
    /// <summary>
    /// Pure, DB-free late/early/worked-minutes calculation (D1 — compute on save,
    /// no posting run). See HR-Payroll-Final-Plan.md section 5.1.
    /// </summary>
    public static class AttendanceCalculator
    {
        public class ShiftInfo
        {
            public TimeSpan TimeIn { get; set; }
            public TimeSpan TimeOut { get; set; }
            public int GraceMinutes { get; set; }
            public int HalfDayMinutes { get; set; }
            public int FullDayMinutes { get; set; }
        }

        /// <summary>
        /// The day's rules, built from the school timing that applies to this
        /// employee. This is the only source now — HrShift was retired, because
        /// a fixed pair of times cannot describe a school week (Friday runs
        /// short, and any single day can move) and because the hours differ by
        /// designation, which a shift could not express either.
        /// </summary>
        /// <remarks>
        /// Grace and the two thresholds come from the timing when the office has
        /// set them. Left empty they are derived from the day's own span, which
        /// keeps the proportions the retired Shift form defaulted to: an 8-hour
        /// (480-minute) day gives 180 minutes for a half day and 360 for a full
        /// one — 37.5% and 75%. So a school running one bell for everyone never
        /// has to fill them in.
        /// </remarks>
        public static ShiftInfo FromSchoolTiming(
            TimeSpan timeIn, TimeSpan timeOut,
            int graceMinutes = 0, int? halfDayMinutes = null, int? fullDayMinutes = null)
        {
            var spanMinutes = (int)Span(timeIn, timeOut).TotalMinutes;

            return new ShiftInfo
            {
                TimeIn = timeIn,
                TimeOut = timeOut,
                GraceMinutes = graceMinutes,
                HalfDayMinutes = halfDayMinutes ?? (int)Math.Round(spanMinutes * 0.375),
                FullDayMinutes = fullDayMinutes ?? (int)Math.Round(spanMinutes * 0.75)
            };
        }

        /// <summary>
        /// How long a stretch from <paramref name="from"/> to <paramref name="to"/>
        /// lasts, allowing for one that runs past midnight.
        /// </summary>
        /// <remarks>
        /// The guard's shift is 7:00 PM to 7:00 AM. Subtracting those two clock
        /// times gives MINUS twelve hours, which made the night shift impossible
        /// to record: the span came out negative, so the half-day and full-day
        /// thresholds were negative, the hours worked clamped to zero, and every
        /// night was graded Absent.
        ///
        /// An end time at or before the start therefore means the next day, and
        /// a day is added. The one thing this cannot express is a shift of 24
        /// hours or more, which is not a shift.
        /// </remarks>
        public static TimeSpan Span(TimeSpan from, TimeSpan to) =>
            to > from ? to - from : to + TimeSpan.FromHours(24) - from;

        /// <summary>True when the stretch runs past midnight into the next day.</summary>
        public static bool IsOvernight(TimeSpan timeIn, TimeSpan timeOut) => timeOut <= timeIn;

        /// <summary>
        /// Minutes from one clock time to another, signed, taking the SHORTER
        /// way round the clock: positive when <paramref name="to"/> comes after
        /// <paramref name="from"/>, negative when it comes before.
        /// </summary>
        /// <remarks>
        /// Plain subtraction cannot answer "how late is this?" on a shift that
        /// crosses midnight. A shift starting at 11:00 PM and an arrival at
        /// 00:30 is ninety minutes late, but subtracting gives minus 22½ hours;
        /// an arrival five minutes EARLY must stay negative so it is not
        /// charged as lateness. Wrapping at the twelve-hour mark gives both:
        /// nobody is twelve hours late, they are early for the next one.
        /// </remarks>
        public static int SignedMinutes(TimeSpan from, TimeSpan to)
        {
            var minutes = (int)(to - from).TotalMinutes;

            if (minutes > 12 * 60) minutes -= 24 * 60;
            else if (minutes < -12 * 60) minutes += 24 * 60;

            return minutes;
        }

        /// <summary>Hours in a working day, for payroll's per-hour components.</summary>
        public static decimal WorkHoursFor(TimeSpan timeIn, TimeSpan timeOut, decimal? configured) =>
            configured ?? Math.Round((decimal)Span(timeIn, timeOut).TotalHours, 2);

        public class Input
        {
            public ShiftInfo? Shift { get; set; }
            public TimeSpan? TimeIn { get; set; }
            public TimeSpan? TimeOut { get; set; }
            public bool IsHoliday { get; set; }
            public bool IsWeeklyOff { get; set; }
            public int? ApprovedLeaveTypeId { get; set; }
        }

        public class Result
        {
            public HrAttendanceStatus Status { get; set; }
            public int LateMinutes { get; set; }
            public int EarlyMinutes { get; set; }
            public int WorkedMinutes { get; set; }
            public int? LeaveTypeId { get; set; }
        }

        public static Result Calculate(Input input)
        {
            if (input.IsHoliday)
                return new Result { Status = HrAttendanceStatus.Holiday };

            if (input.IsWeeklyOff)
                return new Result { Status = HrAttendanceStatus.WeeklyOff };

            if (input.ApprovedLeaveTypeId.HasValue)
                return new Result { Status = HrAttendanceStatus.Leave, LeaveTypeId = input.ApprovedLeaveTypeId };

            if (input.TimeIn is null)
                return new Result { Status = HrAttendanceStatus.Absent };

            if (input.Shift is null)
                throw new InvalidOperationException(
                    "No school timing covers this date, so a time-in cannot be graded. " +
                    "Enter a School Timing range covering it — either for this employee's " +
                    "designation or the school-wide one.");

            var shift = input.Shift;

            // Late, and left early, are both "how far is this clock time from
            // that one" — and both have to work across midnight, because a
            // guard's shift starts at 7:00 PM and ends at 7:00 AM.
            var lateMinutes = Math.Max(0, SignedMinutes(shift.TimeIn + TimeSpan.FromMinutes(shift.GraceMinutes), input.TimeIn.Value));

            var earlyMinutes = input.TimeOut.HasValue
                ? Math.Max(0, SignedMinutes(input.TimeOut.Value, shift.TimeOut))
                : 0;

            // Hours worked: 7:00 PM to 7:00 AM is twelve hours, not minus twelve.
            var workedMinutes = input.TimeOut.HasValue
                ? (int)Span(input.TimeIn.Value, input.TimeOut.Value).TotalMinutes
                : 0;

            // Marked in, so Present. Full stop.
            //
            // This used to grade the day against the half-day and full-day
            // thresholds, so a teacher who arrived late or left early could be
            // recorded as a HALF DAY and lose half a day's pay for twenty
            // minutes. The school's rule is the plain one: if the attendance is
            // marked they were present, and if it is not they were absent. The
            // minutes are not forgiven by this — every one of them is still
            // counted and charged by the late, early and duty components. The
            // penalty is the minutes, not a re-graded day.
            //
            // A genuine half day is still possible: the office marks it on the
            // screen, and an explicit status is never overwritten here.
            var status = HrAttendanceStatus.Present;

            return new Result
            {
                Status = status,
                LateMinutes = lateMinutes,
                EarlyMinutes = earlyMinutes,
                WorkedMinutes = workedMinutes
            };
        }
    }
}

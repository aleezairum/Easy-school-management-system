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
        /// A shift derived from the school timings alone, for staff who have no
        /// shift of their own. A school that runs on one bell for everyone does
        /// not need per-employee shifts, and requiring one just to record a
        /// time-in would make the School Timings screen useless on its own.
        /// </summary>
        /// <remarks>
        /// Grace is 0 — the school timing is the bell, and anyone wanting a
        /// grace period assigns a shift, which then supplies it.
        ///
        /// The day-length thresholds keep the proportions the Shift form itself
        /// defaults to: an 8-hour (480-minute) shift defaults to 180 minutes for
        /// a half day and 360 for a full one, i.e. 37.5% and 75% of the span.
        /// </remarks>
        public static ShiftInfo FromSchoolTiming(TimeSpan timeIn, TimeSpan timeOut)
        {
            var spanMinutes = (int)(timeOut - timeIn).TotalMinutes;

            return new ShiftInfo
            {
                TimeIn = timeIn,
                TimeOut = timeOut,
                GraceMinutes = 0,
                HalfDayMinutes = (int)Math.Round(spanMinutes * 0.375),
                FullDayMinutes = (int)Math.Round(spanMinutes * 0.75)
            };
        }

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
                    "A shift is required to calculate attendance for an employee with a time-in recorded. Assign a shift to the employee first.");

            var shift = input.Shift;

            var lateMinutes = Math.Max(0, (int)(input.TimeIn.Value - (shift.TimeIn + TimeSpan.FromMinutes(shift.GraceMinutes))).TotalMinutes);
            var earlyMinutes = input.TimeOut.HasValue
                ? Math.Max(0, (int)(shift.TimeOut - input.TimeOut.Value).TotalMinutes)
                : 0;
            var workedMinutes = input.TimeOut.HasValue
                ? Math.Max(0, (int)(input.TimeOut.Value - input.TimeIn.Value).TotalMinutes)
                : 0;

            var status = workedMinutes >= shift.FullDayMinutes ? HrAttendanceStatus.Present
                       : workedMinutes >= shift.HalfDayMinutes ? HrAttendanceStatus.HalfDay
                       : HrAttendanceStatus.Absent;

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

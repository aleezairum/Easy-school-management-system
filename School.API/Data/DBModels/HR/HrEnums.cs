namespace School.API.Data.DBModels.HR
{
    // Byte-backed so EF stores them as TINYINT, matching Scripts/HrPayroll.sql.

    public enum HrAttendanceStatus : byte
    {
        Present = 1,
        Absent = 2,
        Leave = 3,
        HalfDay = 4,
        Holiday = 5,
        WeeklyOff = 6,
        ShortLeave = 7,

        // Excused from marking attendance that day — official duty, training, a
        // field visit. Payroll treats it as a worked day: it never counts as
        // absent, and it carries no times so there is no lateness to charge.
        Exempt = 8,

        /// <summary>
        /// Not on the staff that day — before they joined, or after they left.
        /// </summary>
        /// <remarks>
        /// Shown by the month grid and NEVER saved: there is no attendance to
        /// record for a day somebody was not employed, and storing one would
        /// feed payroll a day it must not pay for. It exists so those cells read
        /// as "not theirs" instead of as an unmarked day somebody forgot.
        /// </remarks>
        NotEmployed = 9
    }

    public enum HrAttendanceSource : byte
    {
        Manual = 1,
        Import = 2,

        // A punch from a face-attendance machine (HikvisionEventProcessor).
        Device = 3
    }

    public enum HrLeaveStatus : byte
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3
    }

    public enum HrHolidayType : byte
    {
        Gazetted = 1,
        Vacation = 2,
        HalfDay = 3
    }

    public enum HrPayComponentKind : byte
    {
        Earning = 1,
        Deduction = 2
    }

    public enum HrPayCalcType : byte
    {
        Fixed = 1,
        PercentOf = 2,
        PerLateMinute = 3,
        PerAbsentDay = 4,
        PerOvertimeHour = 5,
        Slab = 6,
        Formula = 7,

        /// <summary>
        /// Charged per minute spent out of school during the working day —
        /// the short leave a teacher takes between arriving and leaving.
        /// Priced exactly like PerLateMinute: grace, per-day cap and free
        /// units all apply, so the school sets what a minute costs.
        /// </summary>
        PerOutingMinute = 8,

        /// <summary>
        /// Charged per minute of leaving before the day ends. The other half of
        /// PerLateMinute: a teacher who arrives twenty minutes late and one who
        /// leaves twenty minutes early have taken the same twenty minutes.
        /// </summary>
        PerEarlyMinute = 9,

        /// <summary>
        /// The duty swap: minutes one member of staff did not work because
        /// somebody else covered for them (short), less the minutes they worked
        /// covering for somebody else (extra).
        /// </summary>
        /// <remarks>
        /// Deliberately ONE component rather than a deduction and an earning.
        /// The amount is simply rate x (short - extra), so it goes NEGATIVE when
        /// a member of staff covered more than was covered for them, and a
        /// negative deduction adds to the pay — the bonus the school asked for.
        ///
        /// It has to be a deduction and not an earning: earnings are added to
        /// gross before the deductions are worked out, and every per-minute rate
        /// here is derived FROM gross. An extra-duty earning would therefore
        /// raise the very rate used to price that person's lateness. A deduction
        /// never touches gross, so the rate stays the rate of their salary.
        /// </remarks>
        PerDutyMinute = 10,

        /// <summary>
        /// Charged per day of leave, at the day's rate - the same price as an
        /// absent day. Each leave day counts by its leave type: the type's
        /// Deduct Days (0 paid, 1 unpaid, 0.5 half), or 1 when the type is
        /// unpaid and has none. A leave day marked with no type counts as 1.
        /// FreeUnitsPerMonth is the number of such days in a month that are
        /// still paid. With no active rule of this type, leave costs nothing.
        /// </summary>
        PerLeaveDay = 11
    }

    public enum HrRateSource : byte
    {
        Fixed = 1,
        Derived = 2
    }

    public enum HrPayBaseOn : byte
    {
        Basic = 1,
        Gross = 2
    }

    public enum HrPayScopeType : byte
    {
        Global = 1,
        Grade = 2,
        Designation = 3,
        Employee = 4
    }

    public enum HrPayrollRunStatus : byte
    {
        Draft = 1,
        Locked = 2,
        Paid = 3
    }

    // DivisorDays is stored as TINYINT: 30 = fixed 30, 0 = calendar days, 255 = working days.
    public static class HrDivisorDays
    {
        public const byte Fixed30 = 30;
        public const byte Calendar = 0;
        public const byte WorkingDays = 255;
    }
}

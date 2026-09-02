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
        Exempt = 8
    }

    public enum HrAttendanceSource : byte
    {
        Manual = 1,
        Import = 2
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
        Formula = 7
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

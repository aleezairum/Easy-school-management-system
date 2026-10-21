namespace School.API.DTOs.HR
{
    public class PayrollRunDto
    {
        public int Id { get; set; }
        public int PayYear { get; set; }
        public byte PayMonth { get; set; }

        /// <summary>Null for a run covering every campus.</summary>
        public int? CampusId { get; set; }
        public string? CampusName { get; set; }

        public byte Status { get; set; }
        public DateTime? GeneratedAt { get; set; }
        public DateTime? LockedAt { get; set; }
        public int EmployeeCount { get; set; }
        public decimal TotalNetPay { get; set; }
    }

    public class GeneratePayrollRunDto
    {
        public int Year { get; set; }
        public int Month { get; set; }

        /// <summary>One campus's salary bill. Null or 0 runs every campus together.</summary>
        public int? CampusId { get; set; }
    }

    public class PayslipLineDto
    {
        public int ComponentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public byte Kind { get; set; }
        public decimal Amount { get; set; }
    }

    public class PayslipDto
    {
        public int Id { get; set; }
        public int RunId { get; set; }
        /// <summary>The run's status (1 Draft, 2 Locked, 3 Paid). A Draft slip
        /// is printed and sent marked "PROOF READ ONLY".</summary>
        public byte RunStatus { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string? DesignationName { get; set; }
        public decimal Basic { get; set; }
        public decimal GrossEarning { get; set; }
        public decimal TotalDeduction { get; set; }
        public decimal NetPay { get; set; }
        public decimal PresentDays { get; set; }
        public decimal AbsentDays { get; set; }
        public decimal LeaveDays { get; set; }
        public int LateMinutes { get; set; }
        public int EarlyMinutes { get; set; }
        public int OutingMinutes { get; set; }

        /// <summary>One minute's cost, and the days and hours it was divided by.</summary>
        public decimal PerMinuteRate { get; set; }
        public decimal RateWorkingDays { get; set; }
        public decimal RateHours { get; set; }

        /// <summary>The month the payslip pays, and the employee's campus.</summary>
        public int PayYear { get; set; }
        public byte PayMonth { get; set; }
        public string? CampusName { get; set; }

        // ── The school's salary sheet columns ────────────────────────
        public decimal TotalDays { get; set; }
        public decimal WorkingDays { get; set; }
        public decimal DailyPay { get; set; }

        // Incentives - each added to the pay and itemised on the slip.
        public decimal Incentive { get; set; }
        public decimal ExtraDuty { get; set; }
        public decimal AcademyDuty { get; set; }
        public decimal Prize { get; set; }
        public decimal SecurityReturn { get; set; }

        public decimal PayDeduction { get; set; }
        public decimal Fine { get; set; }

        /// <summary>Prize - Fine (the old signed column).</summary>
        public decimal PrizeFine { get; set; }

        public decimal SecurityDeduction { get; set; }
        public decimal Advance { get; set; }
        public decimal TakeHome { get; set; }

        /// <summary>Extra days for a month with no day off, and what they paid.</summary>
        public decimal ExtraDays { get; set; }
        public decimal ExtraDaysPay { get; set; }

        public List<PayslipLineDto> Breakdown { get; set; } = new();
        public DateTime? PaidDate { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Reference { get; set; }
    }

    public class PayEmployeeDto
    {
        public DateTime PaidDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? Reference { get; set; }
    }

    public class PayRunDto
    {
        public DateTime PaidDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? Reference { get; set; }
    }

    public class PayrollWarningDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class PayrollRunResultDto
    {
        public PayrollRunDto Run { get; set; } = new();
        public List<PayrollWarningDto> Warnings { get; set; } = new();
    }

    /// <summary>
    /// One employee's hand-entered amounts for one month, as the office types
    /// them on the Monthly Amounts screen.
    /// </summary>
    public class MonthlyAmountDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string? DesignationName { get; set; }
        public string? CampusName { get; set; }
        public decimal Basic { get; set; }

        public decimal Incentive { get; set; }
        public decimal ExtraDuty { get; set; }
        public decimal AcademyDuty { get; set; }
        public decimal SecurityReturn { get; set; }
        public decimal Prize { get; set; }
        public decimal PayDeduction { get; set; }
        public decimal Fine { get; set; }
        public decimal SecurityDeduction { get; set; }

        /// <summary>Read only: the advances dated in this month (Staff
        /// Advances screen). Ignored when the sheet is saved.</summary>
        public decimal Advance { get; set; }
        public string? Remarks { get; set; }
    }

    // ── Staff advances ──────────────────────────────────────────────────

    public class StaffAdvanceDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string? DesignationName { get; set; }
        public string? CampusName { get; set; }
        public DateTime AdvanceDate { get; set; }
        public decimal Amount { get; set; }
        public string? Remarks { get; set; }

        /// <summary>The month's payroll is locked or paid: the advance is on a
        /// finished salary sheet and cannot be changed.</summary>
        public bool Locked { get; set; }
    }

    public class SaveStaffAdvanceDto
    {
        public int EmployeeId { get; set; }
        public DateTime AdvanceDate { get; set; }
        public decimal Amount { get; set; }
        public string? Remarks { get; set; }
    }

    // ── Extra days ──────────────────────────────────────────────────────

    /// <summary>One member of staff on the Extra Days screen, with the reason
    /// they do or do not qualify.</summary>
    public class ExtraDaysRowDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public int? DesignationId { get; set; }
        public string? DesignationName { get; set; }
        public string? CampusName { get; set; }

        public decimal Basic { get; set; }
        public int TotalDays { get; set; }
        public decimal DailyPay { get; set; }

        /// <summary>Working days in the month for this person: holidays and
        /// their weekly off taken out, clipped to when they were on the staff.</summary>
        public int WorkingDays { get; set; }

        public int PresentDays { get; set; }
        public int LeaveDays { get; set; }
        public int AbsentDays { get; set; }
        public int HalfDays { get; set; }

        /// <summary>Working days with no attendance at all.</summary>
        public int NotMarkedDays { get; set; }

        public bool Eligible { get; set; }

        /// <summary>Why not, in the office's words. Null when eligible.</summary>
        public string? Reason { get; set; }

        /// <summary>Saved for this month, else the designation's remembered
        /// figure. Always 0 for somebody who does not qualify.</summary>
        public decimal ExtraDays { get; set; }

        public decimal ExtraDaysPay { get; set; }

        /// <summary>True when a figure has been saved for this month.</summary>
        public bool Saved { get; set; }
    }

    public class ExtraDaysRuleDto
    {
        public int DesignationId { get; set; }
        public string DesignationName { get; set; } = string.Empty;
        public decimal ExtraDays { get; set; }
    }

    public class ExtraDaysSheetDto
    {
        public int Year { get; set; }
        public byte Month { get; set; }
        public int MinWorkingDays { get; set; }
        public List<ExtraDaysRuleDto> Rules { get; set; } = new();
        public List<ExtraDaysRowDto> Rows { get; set; } = new();
    }

    public class SaveExtraDaysDto
    {
        public int Year { get; set; }
        public byte Month { get; set; }
        public int MinWorkingDays { get; set; }
        public List<ExtraDaysRuleDto> Rules { get; set; } = new();
        public List<SaveExtraDaysRowDto> Rows { get; set; } = new();
    }

    public class SaveExtraDaysRowDto
    {
        public int EmployeeId { get; set; }
        public decimal ExtraDays { get; set; }
    }

    public class SaveExtraDaysResultDto
    {
        public int Saved { get; set; }
        public int Cleared { get; set; }

        /// <summary>People the screen sent but who no longer qualify - their
        /// attendance changed after the list was loaded. Not paid.</summary>
        public List<string> Refused { get; set; } = new();
    }

    public class SaveMonthlyAmountsDto
    {
        public int Year { get; set; }
        public byte Month { get; set; }
        public List<MonthlyAmountDto> Rows { get; set; } = new();
    }

    public class SalarySheetRowDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string? DesignationName { get; set; }
        public decimal Basic { get; set; }
        public decimal GrossEarning { get; set; }
        public decimal TotalDeduction { get; set; }
        public decimal NetPay { get; set; }
        public List<PayslipLineDto> Breakdown { get; set; } = new();
    }
}

using NCalc;
using School.API.Data.DBModels.HR;

namespace School.API.Services.Implementations.HR
{
    /// <summary>
    /// Pure, DB-free payroll calculation (D2 — rules in C#, reports in SQL).
    /// See HR-Payroll-Final-Plan.md sections 4 and 5.2/5.3.
    /// </summary>
    public static class PayrollCalculator
    {
        public class SlabBand
        {
            public decimal FromValue { get; set; }
            public decimal? ToValue { get; set; }
            public decimal? Amount { get; set; }
            public decimal? Days { get; set; }
            public decimal? Percent { get; set; }
        }

        public class ComponentRule
        {
            public int ComponentId { get; set; }
            public string Name { get; set; } = string.Empty;
            public HrPayComponentKind Kind { get; set; }
            public HrPayCalcType CalcType { get; set; }
            public decimal? Rate { get; set; }
            public HrRateSource RateSource { get; set; } = HrRateSource.Fixed;
            public HrPayBaseOn? BaseOn { get; set; }
            public byte? DivisorDays { get; set; }
            public decimal? DivisorHours { get; set; }
            public int? GraceMinutes { get; set; }
            public decimal? FreeUnitsPerMonth { get; set; }
            public decimal? CapPerDay { get; set; }
            public decimal? CapPerMonth { get; set; }
            public List<SlabBand> Slabs { get; set; } = new();
            public string? Formula { get; set; }
            public int Priority { get; set; }
            public bool ShowOnPayslip { get; set; } = true;
        }

        public class AttendanceSummary
        {
            /// <summary>Raw late minutes for every day the employee was late, before grace is applied.</summary>
            public List<int> DailyLateMinutes { get; set; } = new();
            public int LateCount => DailyLateMinutes.Count;
            public int TotalLateMinutes => DailyLateMinutes.Sum();

            /// <summary>
            /// Minutes spent out of school during the day, per day — a teacher
            /// who signed out at 11:00 and came back at 12:30 contributes 90.
            /// Kept per day, like lateness, so a daily grace and a daily cap
            /// can be applied before anything is added up.
            /// </summary>
            public List<int> DailyOutingMinutes { get; set; } = new();
            public int OutingCount => DailyOutingMinutes.Count;
            public int TotalOutingMinutes => DailyOutingMinutes.Sum();

            /// <summary>Minutes left before the day ended, per day.</summary>
            public List<int> DailyEarlyMinutes { get; set; } = new();
            public int EarlyCount => DailyEarlyMinutes.Count;
            public int TotalEarlyMinutes => DailyEarlyMinutes.Sum();

            /// <summary>Minutes somebody else covered for this person.</summary>
            public int TotalShortMinutes { get; set; }

            /// <summary>Minutes this person covered for somebody else.</summary>
            public int TotalExtraMinutes { get; set; }
            public decimal AbsentDays { get; set; }
            public decimal PresentDays { get; set; }
            public decimal LeaveDays { get; set; }

            /// <summary>
            /// Leave days weighted by what each costs - its leave type's Deduct
            /// Days, 1 for an unpaid type or a day with no type, 0 for a paid
            /// type. What a PerLeaveDay rule charges for.
            /// </summary>
            public decimal ChargeableLeaveDays { get; set; }
            public decimal OvertimeHours { get; set; }
        }

        public class EmployeeInput
        {
            public int EmployeeId { get; set; }

            /// <summary>Un-prorated monthly basic from the employee's active pay structure.</summary>
            public decimal Basic { get; set; }

            /// <summary>Fixed-amount lines from the employee's pay structure (ComponentsJson).</summary>
            public List<(int ComponentId, decimal Amount)> FixedComponents { get; set; } = new();

            /// <summary>Rule-driven components resolved for this employee, most-specific-wins already applied.</summary>
            public List<ComponentRule> Rules { get; set; } = new();

            public AttendanceSummary Attendance { get; set; } = new();

            /// <summary>Calendar days actually employed this period (join/leave proration). Equals MonthDays for a full month.</summary>
            public decimal PayableDays { get; set; }

            public decimal MonthDays { get; set; }
            public decimal WorkingDays { get; set; }
            public decimal ShiftWorkHours { get; set; } = 8;
        }

        public class PayslipLineResult
        {
            public int ComponentId { get; set; }
            public string Name { get; set; } = string.Empty;
            public HrPayComponentKind Kind { get; set; }
            public decimal Amount { get; set; }
        }

        public class PayrollResult
        {
            public decimal Basic { get; set; }
            public decimal GrossEarning { get; set; }
            public decimal TotalDeduction { get; set; }
            public decimal NetPay { get; set; }
            public List<PayslipLineResult> Breakdown { get; set; } = new();
        }

        public static ComponentRule ToRule(HrPayComponent c) => new()
        {
            ComponentId = c.Id,
            Name = c.Name,
            Kind = c.Kind,
            CalcType = c.CalcType,
            Rate = c.Rate,
            RateSource = c.RateSource,
            BaseOn = c.BaseOn,
            DivisorDays = c.DivisorDays,
            DivisorHours = c.DivisorHours,
            GraceMinutes = c.GraceMinutes,
            FreeUnitsPerMonth = c.FreeUnitsPerMonth,
            CapPerDay = c.CapPerDay,
            CapPerMonth = c.CapPerMonth,
            Slabs = c.Slabs.Select(s => new SlabBand
            {
                FromValue = s.FromValue,
                ToValue = s.ToValue,
                Amount = s.Amount,
                Days = s.Days,
                Percent = s.Percent
            }).ToList(),
            Formula = c.Formula,
            Priority = c.Priority,
            ShowOnPayslip = c.ShowOnPayslip
        };

        /// <summary>
        /// What one day's minutes cost, for the attendance reports and cards:
        /// late arrival, early leaving and time out of school, each priced by
        /// the employee's own rules exactly as <see cref="Calculate"/> prices a
        /// day - same rate, same grace, same daily cap.
        /// </summary>
        /// <remarks>
        /// A rule's FreeUnitsPerMonth and CapPerMonth are month-level and are
        /// not applied to a single day. With the school's rules (no free days,
        /// no monthly cap) a month of these days adds up to the payslip.
        /// </remarks>
        public class DayMinutePricing
        {
            internal readonly List<(HrPayCalcType Type, decimal Rate, int Grace, decimal? CapPerDay)> Rules = new();

            /// <summary>
            /// One minute's cost - the late rule's, or the first minute rule's
            /// when there is no late rule. 0 when no minute rule applies.
            /// </summary>
            public decimal Rate =>
                Rules.Where(r => r.Type == HrPayCalcType.PerLateMinute).Select(r => r.Rate).FirstOrDefault() is var late && late != 0
                    ? late
                    : Rules.Select(r => r.Rate).FirstOrDefault();

            public decimal Late(int minutes) => Charge(HrPayCalcType.PerLateMinute, minutes);
            public decimal Early(int minutes) => Charge(HrPayCalcType.PerEarlyMinute, minutes);
            public decimal Outing(int minutes) => Charge(HrPayCalcType.PerOutingMinute, minutes);

            private decimal Charge(HrPayCalcType type, int minutes)
            {
                if (minutes <= 0) return 0;
                decimal total = 0;
                foreach (var r in Rules.Where(r => r.Type == type))
                {
                    var amount = Math.Max(0, minutes - r.Grace) * r.Rate;
                    if (r.CapPerDay.HasValue) amount = Math.Min(amount, r.CapPerDay.Value);
                    total += amount;
                }
                return total;
            }
        }

        public static DayMinutePricing PriceMinutes(EmployeeInput input)
        {
            var (basic, gross, _) = Earnings(input);
            var pricing = new DayMinutePricing();
            foreach (var rule in input.Rules.Where(r => r.Kind == HrPayComponentKind.Deduction
                     && (r.CalcType == HrPayCalcType.PerLateMinute
                         || r.CalcType == HrPayCalcType.PerEarlyMinute
                         || r.CalcType == HrPayCalcType.PerOutingMinute)))
            {
                pricing.Rules.Add((rule.CalcType, GetPerMinuteRate(rule, basic, gross, input),
                                   rule.GraceMinutes ?? 0, rule.CapPerDay));
            }
            return pricing;
        }

        /// <summary>Prorated basic, gross, and the earning lines - the first half of Calculate.</summary>
        private static (decimal Basic, decimal Gross, List<PayslipLineResult> Lines) Earnings(EmployeeInput input)
        {
            var basic = input.MonthDays > 0
                ? Math.Round(input.Basic * (input.PayableDays / input.MonthDays), 2)
                : input.Basic;

            var breakdown = new List<PayslipLineResult>();
            var fixedIds = input.FixedComponents.Select(c => c.ComponentId).ToHashSet();

            // Fixed lines from the employee's salary structure first.
            decimal gross = basic;
            foreach (var (componentId, amount) in input.FixedComponents)
            {
                var rule = input.Rules.FirstOrDefault(r => r.ComponentId == componentId);
                var kind = rule?.Kind ?? HrPayComponentKind.Earning;
                if (kind == HrPayComponentKind.Earning)
                    gross += amount;

                breakdown.Add(new PayslipLineResult
                {
                    ComponentId = componentId,
                    Name = rule?.Name ?? "Component",
                    Kind = kind,
                    Amount = amount
                });
            }

            // Rule-driven earnings (e.g. overtime) not already covered by a fixed line.
            var earningRules = input.Rules
                .Where(r => r.Kind == HrPayComponentKind.Earning && !fixedIds.Contains(r.ComponentId))
                .OrderBy(r => r.Priority);
            foreach (var rule in earningRules)
            {
                var amount = EvaluateComponent(rule, basic, gross, input);
                gross += amount;
                if (rule.ShowOnPayslip || amount != 0)
                    breakdown.Add(new PayslipLineResult { ComponentId = rule.ComponentId, Name = rule.Name, Kind = HrPayComponentKind.Earning, Amount = amount });
            }

            return (basic, gross, breakdown);
        }

        public static PayrollResult Calculate(EmployeeInput input)
        {
            var (basic, gross, breakdown) = Earnings(input);

            // Deductions apply AFTER gross is final, in Priority order (5.3) — so a
            // percent-of-gross deduction is never affected by another deduction.
            decimal totalDeduction = 0;
            var deductionRules = input.Rules
                .Where(r => r.Kind == HrPayComponentKind.Deduction)
                .OrderBy(r => r.Priority);
            foreach (var rule in deductionRules)
            {
                var amount = EvaluateComponent(rule, basic, gross, input);
                if (rule.CapPerMonth.HasValue)
                    amount = Math.Min(amount, rule.CapPerMonth.Value);

                totalDeduction += amount;
                if (rule.ShowOnPayslip || amount != 0)
                    breakdown.Add(new PayslipLineResult { ComponentId = rule.ComponentId, Name = rule.Name, Kind = HrPayComponentKind.Deduction, Amount = amount });
            }

            var net = Math.Max(0, gross - totalDeduction);

            return new PayrollResult
            {
                Basic = basic,
                GrossEarning = Math.Round(gross, 2),
                TotalDeduction = Math.Round(totalDeduction, 2),
                NetPay = Math.Round(net, 2),
                Breakdown = breakdown
            };
        }

        /// <summary>
        /// Live "employee X, 25 min late -> Rs Y" preview for the Setup screen
        /// (section 4). Evaluates one rule in isolation against a synthetic
        /// attendance summary — used before the rule is wired into any
        /// employee's real payroll run.
        /// </summary>
        public static (decimal Amount, string Explanation) PreviewSingle(
            ComponentRule rule, decimal basic, decimal gross, AttendanceSummary attendance, decimal shiftWorkHours)
        {
            var input = new EmployeeInput
            {
                Basic = basic,
                MonthDays = 30,
                WorkingDays = 26,
                PayableDays = 30,
                ShiftWorkHours = shiftWorkHours,
                Attendance = attendance,
                Rules = new List<ComponentRule> { rule }
            };

            var amount = EvaluateComponent(rule, basic, gross, input);
            if (rule.CapPerMonth.HasValue)
                amount = Math.Min(amount, rule.CapPerMonth.Value);

            var explanation = rule.CalcType switch
            {
                HrPayCalcType.PerLateMinute => $"Rate {GetPerMinuteRate(rule, basic, gross, input):0.####}/min x late minutes beyond {rule.GraceMinutes ?? 0} min grace, capped at {(rule.CapPerDay?.ToString("0.##") ?? "no")}/day.",
                HrPayCalcType.PerOutingMinute => $"Rate {GetPerMinuteRate(rule, basic, gross, input):0.####}/min x minutes out of school beyond {rule.GraceMinutes ?? 0} min grace, capped at {(rule.CapPerDay?.ToString("0.##") ?? "no")}/day.",
                HrPayCalcType.PerAbsentDay => $"Rate {GetPerDayRate(rule, basic, gross, input):0.##}/day x absent days beyond {rule.FreeUnitsPerMonth ?? 0} free.",
                HrPayCalcType.PerLeaveDay => $"Rate {GetPerDayRate(rule, basic, gross, input):0.##}/day x unpaid leave days beyond {rule.FreeUnitsPerMonth ?? 0} paid per month.",
                HrPayCalcType.PercentOf => $"{rule.Rate ?? 0}% of {(rule.BaseOn == HrPayBaseOn.Gross ? "gross" : "basic")}.",
                HrPayCalcType.Fixed => "Fixed amount.",
                HrPayCalcType.Slab => $"Slab band matched on {input.Attendance.LateCount} late day(s) beyond {rule.FreeUnitsPerMonth ?? 0} free.",
                HrPayCalcType.PerOvertimeHour => $"Rate {rule.Rate ?? 0}/hr x {attendance.OvertimeHours} overtime hour(s).",
                HrPayCalcType.Formula => $"Formula: {rule.Formula}",
                _ => string.Empty
            };

            return (Math.Round(amount, 2), explanation);
        }

        private static decimal EvaluateComponent(ComponentRule rule, decimal basic, decimal gross, EmployeeInput input)
        {
            var att = input.Attendance;

            switch (rule.CalcType)
            {
                case HrPayCalcType.Fixed:
                    return rule.Rate ?? 0;

                case HrPayCalcType.PercentOf:
                    {
                        var b = rule.BaseOn == HrPayBaseOn.Gross ? gross : basic;
                        return Math.Round(b * (rule.Rate ?? 0) / 100m, 2);
                    }

                case HrPayCalcType.PerLateMinute:
                    return CalculateLateDeduction(rule, basic, gross, input);

                case HrPayCalcType.PerOutingMinute:
                    return CalculatePerDayMinuteDeduction(rule, basic, gross, input,
                                                          input.Attendance.DailyOutingMinutes);

                case HrPayCalcType.PerEarlyMinute:
                    return CalculatePerDayMinuteDeduction(rule, basic, gross, input,
                                                          input.Attendance.DailyEarlyMinutes);

                case HrPayCalcType.PerDutyMinute:
                    {
                        // short - extra. Negative when this person covered more
                        // than was covered for them, and a negative deduction is
                        // the bonus. Grace and the daily cap have no meaning for
                        // a duty swap, so neither is applied.
                        var net = att.TotalShortMinutes - att.TotalExtraMinutes;
                        var perMinute = GetPerMinuteRate(rule, basic, gross, input);
                        return Math.Round(net * perMinute, 2);
                    }

                case HrPayCalcType.PerAbsentDay:
                    {
                        var freeUnits = rule.FreeUnitsPerMonth ?? 0;
                        var chargeableDays = Math.Max(0, att.AbsentDays - freeUnits);
                        var perDayRate = GetPerDayRate(rule, basic, gross, input);
                        return Math.Round(chargeableDays * perDayRate, 2);
                    }

                case HrPayCalcType.PerLeaveDay:
                    {
                        // The month's first FreeUnitsPerMonth leave days are paid.
                        var paidUnits = rule.FreeUnitsPerMonth ?? 0;
                        var chargeableDays = Math.Max(0, att.ChargeableLeaveDays - paidUnits);
                        return Math.Round(chargeableDays * GetPerDayRate(rule, basic, gross, input), 2);
                    }

                case HrPayCalcType.PerOvertimeHour:
                    return Math.Round(att.OvertimeHours * (rule.Rate ?? 0), 2);

                case HrPayCalcType.Slab:
                    return CalculateSlab(rule, basic, gross, input);

                case HrPayCalcType.Formula:
                    return EvaluateFormula(rule, basic, gross, input);

                default:
                    return 0;
            }
        }

        /// <summary>
        /// Escape hatch (4.3). Variables available: LateMinutes, LateCount,
        /// AbsentDays, WorkedDays (PresentDays), Basic, Gross, MonthDays,
        /// WorkingDays, HoursPerDay, PerDayRate, PerMinuteRate.
        /// </summary>
        private static decimal EvaluateFormula(ComponentRule rule, decimal basic, decimal gross, EmployeeInput input)
        {
            if (string.IsNullOrWhiteSpace(rule.Formula))
                return 0;

            var att = input.Attendance;
            var perDayRate = GetPerDayRate(rule, basic, gross, input);
            var perMinuteRate = GetPerMinuteRate(rule, basic, gross, input);

            var expr = new Expression(rule.Formula)
            {
                Parameters =
                {
                    ["LateMinutes"] = (double)att.TotalLateMinutes,
                    ["OutingMinutes"] = (double)att.TotalOutingMinutes,
                    ["OutingCount"] = (double)att.OutingCount,
                    ["LateCount"] = (double)att.LateCount,
                    ["AbsentDays"] = (double)att.AbsentDays,
                    ["WorkedDays"] = (double)att.PresentDays,
                    ["Basic"] = (double)basic,
                    ["Gross"] = (double)gross,
                    ["MonthDays"] = (double)input.MonthDays,
                    ["WorkingDays"] = (double)input.WorkingDays,
                    ["HoursPerDay"] = (double)input.ShiftWorkHours,
                    ["PerDayRate"] = (double)perDayRate,
                    ["PerMinuteRate"] = (double)perMinuteRate
                }
            };

            var result = expr.Evaluate();
            return Math.Round(Convert.ToDecimal(result), 2);
        }

        /// <summary>
        /// Per-day late deduction with grace and CapPerDay applied per day, then
        /// summed and (by the caller) capped again at CapPerMonth. Aggregating
        /// first and capping once would misstate months with one large outlier
        /// day, so each late day is evaluated independently — this is what the
        /// CapPerDay test case in section 10 is checking.
        /// FreeUnitsPerMonth forgives the smallest N late days in the month
        /// (pure function, no per-day dates to prefer chronologically — this
        /// is the most employee-favourable and simplest deterministic choice).
        /// </summary>
        private static decimal CalculateLateDeduction(ComponentRule rule, decimal basic, decimal gross, EmployeeInput input)
            => CalculatePerDayMinuteDeduction(rule, basic, gross, input, input.Attendance.DailyLateMinutes);

        /// <summary>
        /// Charges a per-minute rate against a list of per-day minute figures —
        /// late arrival, or time spent out of school. Shared so short leave is
        /// priced by exactly the same rules the school already understands for
        /// lateness, rather than a second set that could drift from it.
        /// </summary>
        private static decimal CalculatePerDayMinuteDeduction(
            ComponentRule rule, decimal basic, decimal gross, EmployeeInput input, List<int> dailyMinutes)
        {
            var grace = rule.GraceMinutes ?? 0;
            var rate = GetPerMinuteRate(rule, basic, gross, input);

            var chargeableDays = dailyMinutes
                .Select(m => Math.Max(0, m - grace))
                .Where(m => m > 0)
                .OrderBy(m => m)
                .ToList();

            var freeCount = (int)(rule.FreeUnitsPerMonth ?? 0);
            decimal total = 0;
            foreach (var chargeableMinutes in chargeableDays.Skip(freeCount))
            {
                var dayAmount = chargeableMinutes * rate;
                if (rule.CapPerDay.HasValue)
                    dayAmount = Math.Min(dayAmount, rule.CapPerDay.Value);
                total += dayAmount;
            }

            return Math.Round(total, 2);
        }

        private static decimal CalculateSlab(ComponentRule rule, decimal basic, decimal gross, EmployeeInput input)
        {
            var freeUnits = rule.FreeUnitsPerMonth ?? 0;
            var metric = Math.Max(0, input.Attendance.LateCount - freeUnits);

            var slab = rule.Slabs.FirstOrDefault(s => metric >= s.FromValue && (s.ToValue == null || metric <= s.ToValue));
            if (slab is null) return 0;

            if (slab.Amount.HasValue) return slab.Amount.Value;
            if (slab.Days.HasValue) return Math.Round(slab.Days.Value * GetPerDayRate(rule, basic, gross, input), 2);
            if (slab.Percent.HasValue)
            {
                var b = rule.BaseOn == HrPayBaseOn.Gross ? gross : basic;
                return Math.Round(b * slab.Percent.Value / 100m, 2);
            }
            return 0;
        }

        private static decimal GetPerMinuteRate(ComponentRule rule, decimal basic, decimal gross, EmployeeInput input)
        {
            if (rule.RateSource == HrRateSource.Fixed)
                return rule.Rate ?? 0;

            var b = rule.BaseOn == HrPayBaseOn.Gross ? gross : basic;
            var days = ResolveDivisorDays(rule.DivisorDays, input);
            var hours = rule.DivisorHours ?? input.ShiftWorkHours;
            if (days <= 0 || hours <= 0) return 0;

            return b / days / hours / 60m;
        }

        private static decimal GetPerDayRate(ComponentRule rule, decimal basic, decimal gross, EmployeeInput input)
        {
            if (rule.RateSource == HrRateSource.Fixed)
                return rule.Rate ?? 0;

            var b = rule.BaseOn == HrPayBaseOn.Gross ? gross : basic;
            var days = ResolveDivisorDays(rule.DivisorDays, input);
            return days <= 0 ? 0 : b / days;
        }

        private static decimal ResolveDivisorDays(byte? divisorDays, EmployeeInput input)
        {
            return divisorDays switch
            {
                null => 30m,
                HrDivisorDays.Fixed30 => 30m,
                HrDivisorDays.Calendar => input.MonthDays,
                HrDivisorDays.WorkingDays => input.WorkingDays,
                _ => 30m
            };
        }
    }
}

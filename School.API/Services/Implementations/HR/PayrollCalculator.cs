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
            public decimal AbsentDays { get; set; }
            public decimal PresentDays { get; set; }
            public decimal LeaveDays { get; set; }
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

        public static PayrollResult Calculate(EmployeeInput input)
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
                HrPayCalcType.PerAbsentDay => $"Rate {GetPerDayRate(rule, basic, gross, input):0.##}/day x absent days beyond {rule.FreeUnitsPerMonth ?? 0} free.",
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

                case HrPayCalcType.PerAbsentDay:
                    {
                        var freeUnits = rule.FreeUnitsPerMonth ?? 0;
                        var chargeableDays = Math.Max(0, att.AbsentDays - freeUnits);
                        var perDayRate = GetPerDayRate(rule, basic, gross, input);
                        return Math.Round(chargeableDays * perDayRate, 2);
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
        {
            var grace = rule.GraceMinutes ?? 0;
            var rate = GetPerMinuteRate(rule, basic, gross, input);

            var chargeableDays = input.Attendance.DailyLateMinutes
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

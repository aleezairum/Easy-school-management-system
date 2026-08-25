using School.API.Data.DBModels.HR;
using School.API.Services.Implementations.HR;
using Xunit;
using static School.API.Services.Implementations.HR.PayrollCalculator;

namespace School.Tests
{
    public class PayrollCalculatorTests
    {
        private static EmployeeInput BaseInput(decimal basic, AttendanceSummary? attendance = null, List<ComponentRule>? rules = null) => new()
        {
            EmployeeId = 1,
            Basic = basic,
            FixedComponents = new List<(int, decimal)>(),
            Rules = rules ?? new List<ComponentRule>(),
            Attendance = attendance ?? new AttendanceSummary(),
            PayableDays = 30,
            MonthDays = 30,
            WorkingDays = 26,
            ShiftWorkHours = 7
        };

        // Sir Syed's exact configuration (4.2): per-minute, derived from Basic,
        // Fixed30 days, hours from shift, 10 min grace, Rs 500/day cap.
        private static ComponentRule LateRule(int? capPerDay = 500, decimal? freeUnits = null) => new()
        {
            ComponentId = 1,
            Name = "Late Deduction",
            Kind = HrPayComponentKind.Deduction,
            CalcType = HrPayCalcType.PerLateMinute,
            RateSource = HrRateSource.Derived,
            BaseOn = HrPayBaseOn.Basic,
            DivisorDays = HrDivisorDays.Fixed30,
            DivisorHours = null,
            GraceMinutes = 10,
            CapPerDay = capPerDay,
            FreeUnitsPerMonth = freeUnits
        };

        [Fact]
        public void SirSyed_PerMinuteLate_EndToEnd()
        {
            // Basic 21000, 7 hours/day, 30 days -> perMinuteRate = 21000/30/7/60 = 1.6667
            var input = BaseInput(21000m,
                attendance: new AttendanceSummary { DailyLateMinutes = new List<int> { 25 } }, // 25 min late one day
                rules: new List<ComponentRule> { LateRule() });

            var result = Calculate(input);

            // chargeable = 25 - 10 grace = 15 min; 15 * 1.6667 = 25.0
            var late = result.Breakdown.Single(b => b.ComponentId == 1);
            Assert.Equal(25.00m, late.Amount);
            Assert.Equal(21000m - 25.00m, result.NetPay);
        }

        [Fact]
        public void Grace_FullyForgivesShortLateness()
        {
            var input = BaseInput(21000m,
                attendance: new AttendanceSummary { DailyLateMinutes = new List<int> { 8 } }, // under 10 min grace
                rules: new List<ComponentRule> { LateRule() });

            var result = Calculate(input);
            Assert.DoesNotContain(result.Breakdown, b => b.ComponentId == 1 && b.Amount != 0);
        }

        [Fact]
        public void CapPerDay_LimitsASingleBadDay_EvenWithOtherModerateDays()
        {
            // One huge outlier day (300 min late) plus one moderate day (20 min).
            // Aggregating first would let the cap swallow the moderate day's charge too;
            // per-day evaluation (as implemented) must cap only the outlier.
            var input = BaseInput(50000m,
                attendance: new AttendanceSummary { DailyLateMinutes = new List<int> { 300, 20 } },
                rules: new List<ComponentRule> { LateRule(capPerDay: 500) });

            var result = Calculate(input);
            var rate = 50000m / 30 / 7 / 60;

            var outlierChargeable = 300 - 10;
            var outlierUncapped = outlierChargeable * rate; // > 500
            var moderateChargeable = 20 - 10;
            var moderateAmount = moderateChargeable * rate;

            var expected = Math.Round(500m + moderateAmount, 2); // outlier capped at 500, moderate uncapped
            var actual = result.Breakdown.Single(b => b.ComponentId == 1).Amount;

            Assert.True(outlierUncapped > 500m, "test setup sanity: outlier must exceed the cap uncapped");
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CapPerMonth_LimitsTotalAcrossAllDays()
        {
            var rule = LateRule(capPerDay: null);
            rule.CapPerMonth = 100m;

            var input = BaseInput(21000m,
                attendance: new AttendanceSummary { DailyLateMinutes = new List<int> { 60, 60, 60 } }, // large total
                rules: new List<ComponentRule> { rule });

            var result = Calculate(input);
            var amount = result.Breakdown.Single(b => b.ComponentId == 1).Amount;
            Assert.Equal(100m, amount);
        }

        [Fact]
        public void FreeUnitsPerMonth_ForgivesSmallestLateDaysFirst()
        {
            // 3 late days, 1 free unit -> the smallest chargeable day is dropped.
            var input = BaseInput(21000m,
                attendance: new AttendanceSummary { DailyLateMinutes = new List<int> { 15, 20, 30 } },
                rules: new List<ComponentRule> { LateRule(capPerDay: null, freeUnits: 1) });

            var result = Calculate(input);
            var rate = 21000m / 30 / 7 / 60;

            // chargeable minutes after 10-min grace: 5, 10, 20 -> smallest (5) forgiven -> charge 10+20=30 min
            var expected = Math.Round(30 * rate, 2);
            var actual = result.Breakdown.Single(b => b.ComponentId == 1).Amount;
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Slab_MatchesBandOnLateCountBeyondFreeUnits()
        {
            var rule = new ComponentRule
            {
                ComponentId = 2,
                Name = "Late Slab",
                Kind = HrPayComponentKind.Deduction,
                CalcType = HrPayCalcType.Slab,
                FreeUnitsPerMonth = 3,
                Slabs = new List<SlabBand>
                {
                    new() { FromValue = 0, ToValue = 2, Days = 0 },
                    new() { FromValue = 3, ToValue = null, Days = 0.5m }
                }
            };

            // 5 late days, 3 free -> metric = 2... wait metric uses LateCount directly minus free units.
            var input = BaseInput(30000m,
                attendance: new AttendanceSummary { DailyLateMinutes = new List<int> { 15, 15, 15, 15, 15 } }, // 5 late days
                rules: new List<ComponentRule> { rule });

            var result = Calculate(input);
            // metric = LateCount(5) - FreeUnitsPerMonth(3) = 2 -> falls in first slab (0-2) -> Days=0 -> amount 0
            var amount = result.Breakdown.SingleOrDefault(b => b.ComponentId == 2)?.Amount ?? 0;
            Assert.Equal(0m, amount);
        }

        [Fact]
        public void Slab_HalfDayBandWhenMetricExceedsFreeUnits()
        {
            var rule = new ComponentRule
            {
                ComponentId = 2,
                Name = "Late Slab",
                Kind = HrPayComponentKind.Deduction,
                CalcType = HrPayCalcType.Slab,
                RateSource = HrRateSource.Derived,
                BaseOn = HrPayBaseOn.Basic,
                DivisorDays = HrDivisorDays.Fixed30,
                FreeUnitsPerMonth = 3,
                Slabs = new List<SlabBand>
                {
                    new() { FromValue = 0, ToValue = 0, Days = 0 },
                    new() { FromValue = 1, ToValue = null, Days = 0.5m }
                }
            };

            var input = BaseInput(30000m,
                attendance: new AttendanceSummary { DailyLateMinutes = Enumerable.Repeat(15, 6).ToList() }, // 6 late days
                rules: new List<ComponentRule> { rule });

            var result = Calculate(input);
            // metric = 6 - 3 = 3 -> band [1, null] -> 0.5 day * perDayRate(30000/30=1000) = 500
            var amount = result.Breakdown.Single(b => b.ComponentId == 2).Amount;
            Assert.Equal(500m, amount);
        }

        [Fact]
        public void FixedRate_IgnoresSalaryEntirely()
        {
            var rule = new ComponentRule
            {
                ComponentId = 3,
                Name = "Union Fee",
                Kind = HrPayComponentKind.Deduction,
                CalcType = HrPayCalcType.Fixed,
                Rate = 200m
            };

            var input = BaseInput(50000m, rules: new List<ComponentRule> { rule });
            var result = Calculate(input);

            Assert.Equal(200m, result.Breakdown.Single(b => b.ComponentId == 3).Amount);
            Assert.Equal(50000m - 200m, result.NetPay);
        }

        [Fact]
        public void PercentOfBasic_ComputesCorrectAmount()
        {
            var rule = new ComponentRule
            {
                ComponentId = 4,
                Name = "House Rent",
                Kind = HrPayComponentKind.Earning,
                CalcType = HrPayCalcType.PercentOf,
                BaseOn = HrPayBaseOn.Basic,
                Rate = 40m // 40%
            };

            var input = BaseInput(20000m, rules: new List<ComponentRule> { rule });
            var result = Calculate(input);

            Assert.Equal(8000m, result.Breakdown.Single(b => b.ComponentId == 4).Amount);
            Assert.Equal(28000m, result.GrossEarning);
        }

        [Fact]
        public void MidMonthJoiner_ProratesBasic()
        {
            // Joined on day 16 of a 30-day month -> 15 payable days.
            var input = BaseInput(30000m);
            input.PayableDays = 15;
            input.MonthDays = 30;

            var result = Calculate(input);
            Assert.Equal(15000m, result.Basic);
            Assert.Equal(15000m, result.NetPay);
        }

        [Fact]
        public void NetPay_FlooredAtZero_NeverNegative()
        {
            var rule = new ComponentRule
            {
                ComponentId = 5,
                Name = "Huge Deduction",
                Kind = HrPayComponentKind.Deduction,
                CalcType = HrPayCalcType.Fixed,
                Rate = 999999m
            };

            var input = BaseInput(10000m, rules: new List<ComponentRule> { rule });
            var result = Calculate(input);

            Assert.Equal(0m, result.NetPay);
        }

        [Fact]
        public void DeductionOrder_PercentOfGross_UsesFinalGross_NotAffectedByOtherDeductions()
        {
            // A percent-of-gross deduction must use the FINAL gross (basic + all
            // earnings), not a gross reduced by an earlier deduction (5.3).
            var earning = new ComponentRule
            {
                ComponentId = 6,
                Name = "Allowance",
                Kind = HrPayComponentKind.Earning,
                CalcType = HrPayCalcType.Fixed,
                Rate = 5000m
            };
            var percentDeduction = new ComponentRule
            {
                ComponentId = 7,
                Name = "Tax",
                Kind = HrPayComponentKind.Deduction,
                CalcType = HrPayCalcType.PercentOf,
                BaseOn = HrPayBaseOn.Gross,
                Rate = 10m,
                Priority = 1
            };
            var fixedDeduction = new ComponentRule
            {
                ComponentId = 8,
                Name = "Fine",
                Kind = HrPayComponentKind.Deduction,
                CalcType = HrPayCalcType.Fixed,
                Rate = 1000m,
                Priority = 0 // evaluated first, but must not shrink the base for Tax
            };

            var input = BaseInput(20000m, rules: new List<ComponentRule> { earning, percentDeduction, fixedDeduction });
            var result = Calculate(input);

            // Gross = 20000 + 5000 = 25000. Tax = 10% of 25000 = 2500, NOT 10% of (25000-1000).
            Assert.Equal(25000m, result.GrossEarning);
            Assert.Equal(2500m, result.Breakdown.Single(b => b.ComponentId == 7).Amount);
        }
    }
}

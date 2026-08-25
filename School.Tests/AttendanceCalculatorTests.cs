using School.API.Data.DBModels.HR;
using School.API.Services.Implementations.HR;
using Xunit;

namespace School.Tests
{
    public class AttendanceCalculatorTests
    {
        private static AttendanceCalculator.ShiftInfo StandardShift() => new()
        {
            TimeIn = new TimeSpan(8, 0, 0),
            TimeOut = new TimeSpan(15, 0, 0),
            GraceMinutes = 10,
            HalfDayMinutes = 180,   // 3 hours
            FullDayMinutes = 360    // 6 hours
        };

        [Fact]
        public void OnTime_NoLate_Present()
        {
            var result = AttendanceCalculator.Calculate(new AttendanceCalculator.Input
            {
                Shift = StandardShift(),
                TimeIn = new TimeSpan(8, 0, 0),
                TimeOut = new TimeSpan(15, 0, 0)
            });

            Assert.Equal(HrAttendanceStatus.Present, result.Status);
            Assert.Equal(0, result.LateMinutes);
            Assert.Equal(0, result.EarlyMinutes);
            Assert.Equal(420, result.WorkedMinutes);
        }

        [Fact]
        public void WithinGrace_NoLateCharged()
        {
            var result = AttendanceCalculator.Calculate(new AttendanceCalculator.Input
            {
                Shift = StandardShift(),
                TimeIn = new TimeSpan(8, 9, 0), // 9 min late, grace is 10
                TimeOut = new TimeSpan(15, 0, 0)
            });

            Assert.Equal(0, result.LateMinutes);
            Assert.Equal(HrAttendanceStatus.Present, result.Status);
        }

        [Fact]
        public void PastGrace_LateMinutesChargedBeyondGraceOnly()
        {
            var result = AttendanceCalculator.Calculate(new AttendanceCalculator.Input
            {
                Shift = StandardShift(),
                TimeIn = new TimeSpan(8, 25, 0), // 25 min late, grace 10 -> input.TimeIn - (shift.TimeIn+grace)
                TimeOut = new TimeSpan(15, 0, 0)
            });

            // shift.TimeIn(8:00) + grace(10min) = 8:10; actual 8:25 -> 15 min late
            Assert.Equal(15, result.LateMinutes);
            Assert.Equal(HrAttendanceStatus.Present, result.Status);
        }

        [Fact]
        public void ShortHours_BelowHalfDayThreshold_Absent()
        {
            var result = AttendanceCalculator.Calculate(new AttendanceCalculator.Input
            {
                Shift = StandardShift(),
                TimeIn = new TimeSpan(8, 0, 0),
                TimeOut = new TimeSpan(10, 0, 0) // 120 min worked, below HalfDayMinutes=180
            });

            Assert.Equal(HrAttendanceStatus.Absent, result.Status);
            Assert.Equal(120, result.WorkedMinutes);
        }

        [Fact]
        public void HalfDayHours_BetweenHalfAndFullThreshold_HalfDay()
        {
            var result = AttendanceCalculator.Calculate(new AttendanceCalculator.Input
            {
                Shift = StandardShift(),
                TimeIn = new TimeSpan(8, 0, 0),
                TimeOut = new TimeSpan(12, 0, 0) // 240 min worked: >=180 half day, <360 full day
            });

            Assert.Equal(HrAttendanceStatus.HalfDay, result.Status);
        }

        [Fact]
        public void NoTimeIn_Absent_ZeroEverything()
        {
            var result = AttendanceCalculator.Calculate(new AttendanceCalculator.Input
            {
                Shift = StandardShift(),
                TimeIn = null,
                TimeOut = null
            });

            Assert.Equal(HrAttendanceStatus.Absent, result.Status);
            Assert.Equal(0, result.LateMinutes);
            Assert.Equal(0, result.WorkedMinutes);
        }

        [Fact]
        public void NoTimeOut_ZeroWorkedMinutes_ButLateStillComputed()
        {
            var result = AttendanceCalculator.Calculate(new AttendanceCalculator.Input
            {
                Shift = StandardShift(),
                TimeIn = new TimeSpan(8, 30, 0), // 20 min late beyond grace
                TimeOut = null
            });

            Assert.Equal(20, result.LateMinutes);
            Assert.Equal(0, result.WorkedMinutes);
            Assert.Equal(HrAttendanceStatus.Absent, result.Status); // 0 worked minutes -> below half-day threshold
        }

        [Fact]
        public void Holiday_ShortCircuitsBeforeShiftIsNeeded()
        {
            var result = AttendanceCalculator.Calculate(new AttendanceCalculator.Input
            {
                Shift = null,
                IsHoliday = true,
                TimeIn = new TimeSpan(8, 0, 0)
            });

            Assert.Equal(HrAttendanceStatus.Holiday, result.Status);
            Assert.Equal(0, result.LateMinutes);
        }

        [Fact]
        public void WeeklyOff_ShortCircuits()
        {
            var result = AttendanceCalculator.Calculate(new AttendanceCalculator.Input
            {
                IsWeeklyOff = true
            });

            Assert.Equal(HrAttendanceStatus.WeeklyOff, result.Status);
        }

        [Fact]
        public void ApprovedLeave_ShortCircuitsWithLeaveTypeId()
        {
            var result = AttendanceCalculator.Calculate(new AttendanceCalculator.Input
            {
                ApprovedLeaveTypeId = 3
            });

            Assert.Equal(HrAttendanceStatus.Leave, result.Status);
            Assert.Equal(3, result.LeaveTypeId);
        }

        [Fact]
        public void TimeInWithoutShift_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
                AttendanceCalculator.Calculate(new AttendanceCalculator.Input
                {
                    Shift = null,
                    TimeIn = new TimeSpan(8, 0, 0)
                }));
        }

        [Fact]
        public void EarlyGoing_ComputedWhenLeavingBeforeShiftEnd()
        {
            var result = AttendanceCalculator.Calculate(new AttendanceCalculator.Input
            {
                Shift = StandardShift(),
                TimeIn = new TimeSpan(8, 0, 0),
                TimeOut = new TimeSpan(14, 30, 0) // 30 min early
            });

            Assert.Equal(30, result.EarlyMinutes);
        }
    }
}

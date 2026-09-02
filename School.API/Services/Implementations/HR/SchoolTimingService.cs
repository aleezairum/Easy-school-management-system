using School.API.DTOs.HR;
using School.API.Repositories.Interfaces.HR;
using School.API.Services.Interfaces.HR;

namespace School.API.Services.Implementations.HR
{
    /// <summary>
    /// School timings. The date range is how a timing is ENTERED; the individual
    /// days are what is actually SAVED, and each one can differ.
    /// </summary>
    /// <remarks>
    /// A flat range does not survive a real school week — Friday runs short, and
    /// every so often one particular day moves. So the range plus the Friday
    /// times build a day list, the screen shows that list for editing, and
    /// whatever comes back is written one row per day.
    ///
    /// Holidays and the weekly off days never make it into the list: the school
    /// is shut, so there is no timing for those dates and attendance falls back
    /// to the employee's own shift and weekly-off handling.
    /// </remarks>
    public class SchoolTimingService : ISchoolTimingService
    {
        private readonly ISchoolTimingRepository _repository;
        private readonly IHolidayRepository _holidayRepository;

        // A whole school year is ~365 rows; anything far beyond that is a typo in
        // the date fields, not a real range, and expanding it would quietly write
        // tens of thousands of rows.
        private const int MaxRangeDays = 1100;

        public SchoolTimingService(ISchoolTimingRepository repository, IHolidayRepository holidayRepository)
        {
            _repository = repository;
            _holidayRepository = holidayRepository;
        }

        public Task<List<SchoolTimingDto>> GetAllAsync(int? year = null) => _repository.GetAllAsync(year);
        public Task<SchoolTimingDto?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);
        public Task<List<SchoolTimingDayDto>> GetDaysAsync(int timingId) => _repository.GetDaysAsync(timingId);

        public async Task<List<SchoolTimingDayDto>> ExpandAsync(
            DateTime startDate, DateTime endDate,
            TimeSpan timeIn, TimeSpan timeOut,
            TimeSpan? fridayTimeIn, TimeSpan? fridayTimeOut,
            byte? offDay1, byte? offDay2)
        {
            if (endDate.Date < startDate.Date) return new List<SchoolTimingDayDto>();
            if ((endDate.Date - startDate.Date).TotalDays + 1 > MaxRangeDays)
                throw new InvalidOperationException(
                    $"That range covers more than {MaxRangeDays:N0} days. Narrow it down.");

            var days = await SchoolDaysAsync(startDate, endDate, offDay1, offDay2);

            return days.Select(d =>
            {
                var (tIn, tOut) = TimesFor(d, timeIn, timeOut, fridayTimeIn, fridayTimeOut);
                return new SchoolTimingDayDto
                {
                    Id = 0,
                    TDate = d,
                    DayOfWeek = (byte)((int)d.DayOfWeek + 1),
                    TimeIn = tIn,
                    TimeOut = tOut,
                    IsOverridden = false
                };
            }).ToList();
        }

        public async Task<SchoolTimingDto> CreateAsync(SaveSchoolTimingDto dto, int userId)
        {
            Validate(dto);

            var entity = await _repository.CreateAsync(dto, userId);
            await ApplyAsync(entity.Id, dto);

            return (await _repository.GetByIdAsync(entity.Id))!;
        }

        public async Task<SchoolTimingDto?> UpdateAsync(int id, SaveSchoolTimingDto dto, int userId)
        {
            Validate(dto);

            var entity = await _repository.UpdateAsync(id, dto, userId);
            if (entity == null) return null;

            await ApplyAsync(id, dto);

            return await _repository.GetByIdAsync(id);
        }

        public Task<bool> DeleteAsync(int id) => _repository.DeleteAsync(id);

        public async Task<SchoolTimingDayDto?> UpdateDayAsync(int dayId, SaveSchoolTimingDayTimeDto dto)
        {
            if (dto.TimeOut <= dto.TimeIn)
                throw new InvalidOperationException("End time must be after the start time.");

            return await _repository.UpdateDayAsync(dayId, dto);
        }

        /// <summary>
        /// Puts one day back on the times its range gives it — including the
        /// Friday times, if that day is a Friday and the range has them.
        /// </summary>
        public async Task<SchoolTimingDayDto?> ResetDayAsync(int dayId)
        {
            var day = await _repository.GetDayAsync(dayId);
            if (day == null) return null;

            var timing = await _repository.GetByIdAsync(day.TimingId);
            if (timing == null) return null;

            var (timeIn, timeOut) = TimesFor(day.TDate, timing.TimeIn, timing.TimeOut,
                                             timing.FridayTimeIn, timing.FridayTimeOut);

            return await _repository.ResetDayAsync(dayId, timeIn, timeOut);
        }

        /// <summary>
        /// An inactive timing is applied to no days at all — that is what makes
        /// the Active switch mean something: turn it off and those dates go back
        /// to the employees' own shifts.
        /// </summary>
        private async Task ApplyAsync(int timingId, SaveSchoolTimingDto dto)
        {
            if (!dto.IsActive)
            {
                await _repository.ApplyDaysAsync(timingId, new List<SchoolTimingDayPlan>(), preserveEdited: false);
                return;
            }

            // The screen sends the days it showed, so they are saved as given and
            // nothing is preserved behind the user's back — they just looked at
            // every row. Without a list the range is expanded instead, and then a
            // day someone edited on its own is worth keeping.
            var explicitDays = dto.Days.Count > 0;

            var plan = explicitDays
                ? await PlanFromDaysAsync(dto)
                : (await SchoolDaysAsync(dto.StartDate, dto.EndDate, dto.OffDay1, dto.OffDay2))
                    .Select(d =>
                    {
                        var (tIn, tOut) = TimesFor(d, dto.TimeIn, dto.TimeOut, dto.FridayTimeIn, dto.FridayTimeOut);
                        return new SchoolTimingDayPlan { Date = d, TimeIn = tIn, TimeOut = tOut, IsOverridden = false };
                    })
                    .ToList();

            await _repository.ApplyDaysAsync(timingId, plan, preserveEdited: !explicitDays);
        }

        private async Task<List<SchoolTimingDayPlan>> PlanFromDaysAsync(SaveSchoolTimingDto dto)
        {
            var from = dto.StartDate.Date;
            var to = dto.EndDate.Date;
            var holidays = await _holidayRepository.GetActiveDatesAsync(from, to);

            var plan = new List<SchoolTimingDayPlan>();
            var seen = new HashSet<DateTime>();

            foreach (var day in dto.Days)
            {
                var date = day.TDate.Date;

                // Anything outside the range, a duplicate, a holiday or a weekly
                // off is dropped rather than trusted: the invariants are the
                // server's to hold.
                if (date < from || date > to) continue;
                if (holidays.Contains(date)) continue;
                if (IsWeeklyOff(date, dto.OffDay1, dto.OffDay2)) continue;
                if (!seen.Add(date)) continue;

                if (day.TimeOut <= day.TimeIn)
                    throw new InvalidOperationException(
                        $"{date:dd MMM yyyy}: end time must be after the start time.");

                // "Overridden" is worked out here rather than trusted from the
                // client: it simply means this day does not match what the range
                // and the Friday rule would have given it.
                var (ruleIn, ruleOut) = TimesFor(date, dto.TimeIn, dto.TimeOut, dto.FridayTimeIn, dto.FridayTimeOut);

                plan.Add(new SchoolTimingDayPlan
                {
                    Date = date,
                    TimeIn = day.TimeIn,
                    TimeOut = day.TimeOut,
                    IsOverridden = day.TimeIn != ruleIn || day.TimeOut != ruleOut
                });
            }

            return plan;
        }

        private static (TimeSpan In, TimeSpan Out) TimesFor(
            DateTime date, TimeSpan timeIn, TimeSpan timeOut, TimeSpan? fridayIn, TimeSpan? fridayOut)
        {
            return date.DayOfWeek == System.DayOfWeek.Friday && fridayIn.HasValue && fridayOut.HasValue
                ? (fridayIn.Value, fridayOut.Value)
                : (timeIn, timeOut);
        }

        /// <summary>
        /// The dates in a range the school is actually open: holidays and the
        /// weekly off days drop out, because there is no timing to give them.
        /// </summary>
        private async Task<List<DateTime>> SchoolDaysAsync(
            DateTime startDate, DateTime endDate, byte? offDay1, byte? offDay2)
        {
            var from = startDate.Date;
            var to = endDate.Date;

            var holidays = await _holidayRepository.GetActiveDatesAsync(from, to);

            var days = new List<DateTime>();
            for (var d = from; d <= to; d = d.AddDays(1))
            {
                if (holidays.Contains(d)) continue;
                if (IsWeeklyOff(d, offDay1, offDay2)) continue;
                days.Add(d);
            }
            return days;
        }

        // 1 = Sunday .. 7 = Saturday, the convention HrShift and Employees use.
        private static bool IsWeeklyOff(DateTime date, byte? offDay1, byte? offDay2)
        {
            var dow = (byte)((int)date.DayOfWeek + 1);
            return dow == offDay1 || dow == offDay2;
        }

        private static void Validate(SaveSchoolTimingDto dto)
        {
            if (dto.EndDate.Date < dto.StartDate.Date)
                throw new InvalidOperationException("End date cannot be before the start date.");

            if (dto.TimeOut <= dto.TimeIn)
                throw new InvalidOperationException("School end time must be after the start time.");

            if (dto.FridayTimeIn.HasValue != dto.FridayTimeOut.HasValue)
                throw new InvalidOperationException("Set both Friday times, or neither.");

            if (dto.FridayTimeIn.HasValue && dto.FridayTimeOut!.Value <= dto.FridayTimeIn.Value)
                throw new InvalidOperationException("Friday end time must be after the Friday start time.");

            if (dto.OffDay1.HasValue && dto.OffDay2.HasValue && dto.OffDay1 == dto.OffDay2)
                throw new InvalidOperationException("The two off days cannot be the same day.");

            // A Friday that is also the weekly off gets no day row at all, so the
            // Friday times would silently do nothing. Say so rather than saving a
            // setting that cannot take effect.
            const byte friday = 6;
            if (dto.FridayTimeIn.HasValue && (dto.OffDay1 == friday || dto.OffDay2 == friday))
                throw new InvalidOperationException(
                    "Friday is set as a weekly off, so it gets no timing at all. Remove it from the off days, or clear the Friday times.");

            var days = (dto.EndDate.Date - dto.StartDate.Date).TotalDays + 1;
            if (days > MaxRangeDays)
                throw new InvalidOperationException(
                    $"That range covers {days:N0} days. Please keep a single school timing under {MaxRangeDays:N0} days.");
        }
    }
}

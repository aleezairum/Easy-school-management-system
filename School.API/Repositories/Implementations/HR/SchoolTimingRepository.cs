using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.HR;
using School.API.DTOs.HR;
using School.API.Repositories.Interfaces.HR;

namespace School.API.Repositories.Implementations.HR
{
    public class SchoolTimingRepository : ISchoolTimingRepository
    {
        private readonly SchoolDbContext _context;

        public SchoolTimingRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<List<SchoolTimingDto>> GetAllAsync(int? year = null)
        {
            var query = _context.Set<HrSchoolTiming>().AsNoTracking();
            if (year.HasValue)
                query = query.Where(t => t.StartDate.Year <= year.Value && t.EndDate.Year >= year.Value);

            var timings = await query.OrderByDescending(t => t.StartDate).ToListAsync();
            if (timings.Count == 0) return new List<SchoolTimingDto>();

            // The day rows are counted in memory rather than as a correlated
            // subquery per row: this table holds one row per school day, so it is
            // small, and the counting rule (who owns a date inside someone else's
            // range) is easier to read here than in a projection.
            var days = await _context.Set<HrSchoolTimingDay>()
                .AsNoTracking()
                .Select(d => new { d.TimingId, d.TDate, d.IsOverridden, d.DesignationId })
                .ToListAsync();

            return timings.Select(t => ToDto(
                t,
                days.Count(d => d.TimingId == t.Id),
                days.Count(d => d.TimingId == t.Id && d.IsOverridden),
                // "Taken over by a later range" only counts within the SAME
                // designation. A Principal's range covering the same dates as a
                // Teacher's does not override it — they are different clocks.
                days.Count(d => d.TimingId != t.Id
                                && d.DesignationId == t.DesignationId
                                && d.TDate >= t.StartDate && d.TDate <= t.EndDate)))
                .ToList();
        }

        public async Task<SchoolTimingDto?> GetByIdAsync(int id)
        {
            var entity = await _context.Set<HrSchoolTiming>().AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
            if (entity == null) return null;

            var applied = await _context.Set<HrSchoolTimingDay>().CountAsync(d => d.TimingId == id);
            var edited = await _context.Set<HrSchoolTimingDay>().CountAsync(d => d.TimingId == id && d.IsOverridden);
            var overridden = await _context.Set<HrSchoolTimingDay>()
                .CountAsync(d => d.TimingId != id
                                 && d.DesignationId == entity.DesignationId
                                 && d.TDate >= entity.StartDate && d.TDate <= entity.EndDate);

            return ToDto(entity, applied, edited, overridden);
        }

        public async Task<List<SchoolTimingDayDto>> GetDaysAsync(int timingId)
        {
            var days = await _context.Set<HrSchoolTimingDay>()
                .AsNoTracking()
                .Where(d => d.TimingId == timingId)
                .OrderBy(d => d.TDate)
                .ToListAsync();

            return days.Select(ToDayDto).ToList();
        }

        public async Task<HrSchoolTiming> CreateAsync(SaveSchoolTimingDto dto, int userId)
        {
            var entity = new HrSchoolTiming
            {
                Name = dto.Name,
                DesignationId = dto.DesignationId,
                GraceMinutes = dto.GraceMinutes,
                HalfDayMinutes = dto.HalfDayMinutes,
                FullDayMinutes = dto.FullDayMinutes,
                WorkHours = dto.WorkHours,
                StartDate = dto.StartDate.Date,
                EndDate = dto.EndDate.Date,
                TimeIn = dto.TimeIn,
                TimeOut = dto.TimeOut,
                FridayTimeIn = dto.FridayTimeIn,
                FridayTimeOut = dto.FridayTimeOut,
                OffDay1 = dto.OffDay1,
                OffDay2 = dto.OffDay2,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            _context.Set<HrSchoolTiming>().Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<HrSchoolTiming?> UpdateAsync(int id, SaveSchoolTimingDto dto, int userId)
        {
            var entity = await _context.Set<HrSchoolTiming>().FirstOrDefaultAsync(t => t.Id == id);
            if (entity == null) return null;

            entity.Name = dto.Name;
            entity.DesignationId = dto.DesignationId;
            entity.GraceMinutes = dto.GraceMinutes;
            entity.HalfDayMinutes = dto.HalfDayMinutes;
            entity.FullDayMinutes = dto.FullDayMinutes;
            entity.WorkHours = dto.WorkHours;
            entity.StartDate = dto.StartDate.Date;
            entity.EndDate = dto.EndDate.Date;
            entity.TimeIn = dto.TimeIn;
            entity.TimeOut = dto.TimeOut;
            entity.FridayTimeIn = dto.FridayTimeIn;
            entity.FridayTimeOut = dto.FridayTimeOut;
            entity.OffDay1 = dto.OffDay1;
            entity.OffDay2 = dto.OffDay2;
            entity.IsActive = dto.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = userId;

            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.Set<HrSchoolTiming>().FirstOrDefaultAsync(t => t.Id == id);
            if (entity == null) return false;

            // The day rows go with it (FK ON DELETE CASCADE), so those dates fall
            // back to each employee's own shift.
            _context.Set<HrSchoolTiming>().Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> ApplyDaysAsync(
            int timingId, IReadOnlyCollection<SchoolTimingDayPlan> plan, bool preserveEdited)
        {
            // Everything below is scoped to THIS range's designation. Without
            // that, saving the Teachers' timing would delete the Principal's day
            // rows for the same dates, because those dates are in the plan.
            var designationId = await _context.Set<HrSchoolTiming>()
                .Where(t => t.Id == timingId)
                .Select(t => t.DesignationId)
                .FirstOrDefaultAsync();

            var dates = plan.Select(p => p.Date.Date).ToList();
            var dateSet = dates.ToHashSet();

            // preserveEdited is for the range-only save: a day someone changed on
            // its own is kept, because re-entering the range around it was never
            // meant to undo it. When the caller sends the day list explicitly they
            // have just seen every row, so that list wins outright.
            var kept = preserveEdited
                ? (await _context.Set<HrSchoolTimingDay>()
                        .Where(d => d.TimingId == timingId && d.IsOverridden)
                        .Select(d => d.TDate)
                        .ToListAsync())
                    .Where(dateSet.Contains)
                    .ToHashSet()
                : new HashSet<DateTime>();

            var stale = await _context.Set<HrSchoolTimingDay>()
                .Where(d => d.DesignationId == designationId
                            && (d.TimingId == timingId || dates.Contains(d.TDate)))
                .ToListAsync();

            var toRemove = stale.Where(d => !kept.Contains(d.TDate)).ToList();
            if (toRemove.Count > 0)
                _context.Set<HrSchoolTimingDay>().RemoveRange(toRemove);

            var rows = plan
                .Where(p => !kept.Contains(p.Date.Date))
                .Select(p => new HrSchoolTimingDay
                {
                    TimingId = timingId,
                    DesignationId = designationId,
                    TDate = p.Date.Date,
                    TimeIn = p.TimeIn,
                    TimeOut = p.TimeOut,
                    IsOverridden = p.IsOverridden
                })
                .ToList();

            _context.Set<HrSchoolTimingDay>().AddRange(rows);
            await _context.SaveChangesAsync();

            return rows.Count + kept.Count;
        }

        public async Task<SchoolTimingDayDto?> UpdateDayAsync(int dayId, SaveSchoolTimingDayTimeDto dto)
        {
            var day = await _context.Set<HrSchoolTimingDay>().FirstOrDefaultAsync(d => d.Id == dayId);
            if (day == null) return null;

            day.TimeIn = dto.TimeIn;
            day.TimeOut = dto.TimeOut;
            day.IsOverridden = true;

            await _context.SaveChangesAsync();
            return ToDayDto(day);
        }

        public async Task<SchoolTimingDayDto?> ResetDayAsync(int dayId, TimeSpan timeIn, TimeSpan timeOut)
        {
            var day = await _context.Set<HrSchoolTimingDay>().FirstOrDefaultAsync(d => d.Id == dayId);
            if (day == null) return null;

            day.TimeIn = timeIn;
            day.TimeOut = timeOut;
            day.IsOverridden = false;

            await _context.SaveChangesAsync();
            return ToDayDto(day);
        }

        public Task<HrSchoolTimingDay?> GetDayAsync(int dayId) =>
            _context.Set<HrSchoolTimingDay>().AsNoTracking().FirstOrDefaultAsync(d => d.Id == dayId);

        public async Task<Dictionary<(DateTime Date, int? DesignationId), HrSchoolTimingDay>> GetDaysByDateAsync(
            DateTime from, DateTime to)
        {
            var days = await _context.Set<HrSchoolTimingDay>()
                .AsNoTracking()
                .Include(d => d.Timing)   // the grace and threshold rules live on the range
                .Where(d => d.TDate >= from.Date && d.TDate <= to.Date)
                .ToListAsync();

            return days.ToDictionary(d => (d.TDate, d.DesignationId));
        }

        public async Task<Dictionary<int, (byte? OffDay1, byte? OffDay2)>> GetOffDaysAsync(
            DateTime from, DateTime to)
        {
            var ranges = await _context.Set<HrSchoolTiming>()
                .AsNoTracking()
                .Where(t => t.IsActive && t.StartDate <= to.Date && t.EndDate >= from.Date)
                .Select(t => new { t.DesignationId, t.StartDate, t.Id, t.OffDay1, t.OffDay2 })
                .ToListAsync();

            // Newest range wins where two overlap the same period — the same
            // rule the day rows follow, so the off days cannot disagree with them.
            return ranges
                // 0 is the school-wide bucket: Dictionary will not take a null key.
                .GroupBy(t => t.DesignationId ?? 0)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var latest = g.OrderByDescending(t => t.StartDate).ThenByDescending(t => t.Id).First();
                        return (latest.OffDay1, latest.OffDay2);
                    });
        }

        private static SchoolTimingDayDto ToDayDto(HrSchoolTimingDay d) => new()
        {
            Id = d.Id,
            TDate = d.TDate,
            DayOfWeek = (byte)((int)d.TDate.DayOfWeek + 1),  // 1 = Sunday .. 7 = Saturday
            TimeIn = d.TimeIn,
            TimeOut = d.TimeOut,
            IsOverridden = d.IsOverridden
        };

        private static SchoolTimingDto ToDto(HrSchoolTiming t, int daysApplied, int daysEdited, int daysOverridden) => new()
        {
            Id = t.Id,
            Name = t.Name,
            DesignationId = t.DesignationId,
            GraceMinutes = t.GraceMinutes,
            HalfDayMinutes = t.HalfDayMinutes,
            FullDayMinutes = t.FullDayMinutes,
            WorkHours = t.WorkHours,
            StartDate = t.StartDate,
            EndDate = t.EndDate,
            TimeIn = t.TimeIn,
            TimeOut = t.TimeOut,
            FridayTimeIn = t.FridayTimeIn,
            FridayTimeOut = t.FridayTimeOut,
            OffDay1 = t.OffDay1,
            OffDay2 = t.OffDay2,
            IsActive = t.IsActive,
            DaysApplied = daysApplied,
            DaysEdited = daysEdited,
            DaysOverridden = daysOverridden,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };
    }
}

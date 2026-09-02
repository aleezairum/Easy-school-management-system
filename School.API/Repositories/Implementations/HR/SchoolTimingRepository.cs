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
                .Select(d => new { d.TimingId, d.TDate, d.IsOverridden })
                .ToListAsync();

            return timings.Select(t => ToDto(
                t,
                days.Count(d => d.TimingId == t.Id),
                days.Count(d => d.TimingId == t.Id && d.IsOverridden),
                days.Count(d => d.TimingId != t.Id && d.TDate >= t.StartDate && d.TDate <= t.EndDate)))
                .ToList();
        }

        public async Task<SchoolTimingDto?> GetByIdAsync(int id)
        {
            var entity = await _context.Set<HrSchoolTiming>().AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
            if (entity == null) return null;

            var applied = await _context.Set<HrSchoolTimingDay>().CountAsync(d => d.TimingId == id);
            var edited = await _context.Set<HrSchoolTimingDay>().CountAsync(d => d.TimingId == id && d.IsOverridden);
            var overridden = await _context.Set<HrSchoolTimingDay>()
                .CountAsync(d => d.TimingId != id && d.TDate >= entity.StartDate && d.TDate <= entity.EndDate);

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
                .Where(d => d.TimingId == timingId || dates.Contains(d.TDate))
                .ToListAsync();

            var toRemove = stale.Where(d => !kept.Contains(d.TDate)).ToList();
            if (toRemove.Count > 0)
                _context.Set<HrSchoolTimingDay>().RemoveRange(toRemove);

            var rows = plan
                .Where(p => !kept.Contains(p.Date.Date))
                .Select(p => new HrSchoolTimingDay
                {
                    TimingId = timingId,
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

        public async Task<Dictionary<DateTime, HrSchoolTimingDay>> GetDaysByDateAsync(DateTime from, DateTime to)
        {
            return await _context.Set<HrSchoolTimingDay>()
                .AsNoTracking()
                .Where(d => d.TDate >= from.Date && d.TDate <= to.Date)
                .ToDictionaryAsync(d => d.TDate);
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

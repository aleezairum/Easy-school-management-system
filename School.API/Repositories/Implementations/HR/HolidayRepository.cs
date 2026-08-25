using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.HR;
using School.API.DTOs.HR;
using School.API.Repositories.Interfaces.HR;

namespace School.API.Repositories.Implementations.HR
{
    public class HolidayRepository : IHolidayRepository
    {
        private readonly SchoolDbContext _context;

        public HolidayRepository(SchoolDbContext context)
        {
            _context = context;
        }

        private static HolidayDto ToDto(HrHoliday h) => new()
        {
            Id = h.Id,
            HDate = h.HDate,
            Name = h.Name,
            HType = (byte)h.HType,
            IsActive = h.IsActive
        };

        public async Task<List<HolidayDto>> GetAllAsync(int? year = null)
        {
            var query = _context.Set<HrHoliday>().AsNoTracking().Where(h => h.IsActive);
            if (year.HasValue)
                query = query.Where(h => h.HDate.Year == year.Value);

            return await query.OrderBy(h => h.HDate).Select(h => ToDto(h)).ToListAsync();
        }

        public async Task<HolidayDto?> GetByIdAsync(int id)
        {
            var entity = await _context.Set<HrHoliday>().AsNoTracking().FirstOrDefaultAsync(h => h.Id == id);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<HrHoliday> CreateAsync(SaveHolidayDto dto)
        {
            var entity = new HrHoliday
            {
                HDate = dto.HDate.Date,
                Name = dto.Name,
                HType = (HrHolidayType)dto.HType,
                IsActive = dto.IsActive
            };
            _context.Set<HrHoliday>().Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<HrHoliday?> UpdateAsync(int id, SaveHolidayDto dto)
        {
            var entity = await _context.Set<HrHoliday>().FirstOrDefaultAsync(h => h.Id == id);
            if (entity == null) return null;

            entity.HDate = dto.HDate.Date;
            entity.Name = dto.Name;
            entity.HType = (HrHolidayType)dto.HType;
            entity.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.Set<HrHoliday>().FirstOrDefaultAsync(h => h.Id == id);
            if (entity == null) return false;

            _context.Set<HrHoliday>().Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> CopyYearAsync(int fromYear, int toYear)
        {
            var source = await _context.Set<HrHoliday>()
                .AsNoTracking()
                .Where(h => h.IsActive && h.HDate.Year == fromYear)
                .ToListAsync();

            var yearDiff = toYear - fromYear;
            var existingDates = await _context.Set<HrHoliday>()
                .AsNoTracking()
                .Where(h => h.IsActive && h.HDate.Year == toYear)
                .Select(h => h.HDate)
                .ToListAsync();
            var existingSet = existingDates.ToHashSet();

            var toAdd = new List<HrHoliday>();
            foreach (var h in source)
            {
                var newDate = h.HDate.AddYears(yearDiff);
                if (existingSet.Contains(newDate)) continue;
                toAdd.Add(new HrHoliday { HDate = newDate, Name = h.Name, HType = h.HType, IsActive = true });
            }

            if (toAdd.Count > 0)
            {
                _context.Set<HrHoliday>().AddRange(toAdd);
                await _context.SaveChangesAsync();
            }
            return toAdd.Count;
        }

        public async Task<HashSet<DateTime>> GetActiveDatesAsync(DateTime from, DateTime to)
        {
            var dates = await _context.Set<HrHoliday>()
                .AsNoTracking()
                .Where(h => h.IsActive && h.HDate >= from.Date && h.HDate <= to.Date)
                .Select(h => h.HDate)
                .ToListAsync();
            return dates.ToHashSet();
        }

        public async Task<Dictionary<DateTime, string>> GetActiveNamesAsync(DateTime from, DateTime to)
        {
            return await _context.Set<HrHoliday>()
                .AsNoTracking()
                .Where(h => h.IsActive && h.HDate >= from.Date && h.HDate <= to.Date)
                .ToDictionaryAsync(h => h.HDate, h => h.Name);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.HR;
using School.API.DTOs.HR;
using School.API.Repositories.Interfaces.HR;

namespace School.API.Repositories.Implementations.HR
{
    public class ShiftRepository : IShiftRepository
    {
        private readonly SchoolDbContext _context;

        public ShiftRepository(SchoolDbContext context)
        {
            _context = context;
        }

        private static ShiftDto ToDto(HrShift s) => new()
        {
            Id = s.Id,
            Code = s.Code,
            Name = s.Name,
            TimeIn = s.TimeIn,
            TimeOut = s.TimeOut,
            GraceMinutes = s.GraceMinutes,
            WorkHours = s.WorkHours,
            HalfDayMinutes = s.HalfDayMinutes,
            FullDayMinutes = s.FullDayMinutes,
            OffDay1 = s.OffDay1,
            OffDay2 = s.OffDay2,
            EffectiveFrom = s.EffectiveFrom,
            EffectiveTo = s.EffectiveTo,
            IsActive = s.IsActive
        };

        public async Task<List<ShiftDto>> GetAllAsync()
        {
            return await _context.Set<HrShift>()
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .Select(s => ToDto(s))
                .ToListAsync();
        }

        public async Task<ShiftDto?> GetByIdAsync(int id)
        {
            var entity = await _context.Set<HrShift>().AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<List<ShiftDropdownDto>> GetDropdownAsync()
        {
            return await _context.Set<HrShift>()
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .Select(s => new ShiftDropdownDto { Id = s.Id, Code = s.Code, Name = s.Name })
                .ToListAsync();
        }

        public async Task<HrShift?> GetEntityByIdAsync(int id)
        {
            return await _context.Set<HrShift>().AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<HrShift> CreateAsync(SaveShiftDto dto)
        {
            var entity = new HrShift
            {
                Code = dto.Code,
                Name = dto.Name,
                TimeIn = dto.TimeIn,
                TimeOut = dto.TimeOut,
                GraceMinutes = dto.GraceMinutes,
                WorkHours = dto.WorkHours,
                HalfDayMinutes = dto.HalfDayMinutes,
                FullDayMinutes = dto.FullDayMinutes,
                OffDay1 = dto.OffDay1,
                OffDay2 = dto.OffDay2,
                EffectiveFrom = dto.EffectiveFrom,
                EffectiveTo = dto.EffectiveTo,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };
            _context.Set<HrShift>().Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<HrShift?> UpdateAsync(int id, SaveShiftDto dto)
        {
            var entity = await _context.Set<HrShift>().FirstOrDefaultAsync(s => s.Id == id);
            if (entity == null) return null;

            entity.Code = dto.Code;
            entity.Name = dto.Name;
            entity.TimeIn = dto.TimeIn;
            entity.TimeOut = dto.TimeOut;
            entity.GraceMinutes = dto.GraceMinutes;
            entity.WorkHours = dto.WorkHours;
            entity.HalfDayMinutes = dto.HalfDayMinutes;
            entity.FullDayMinutes = dto.FullDayMinutes;
            entity.OffDay1 = dto.OffDay1;
            entity.OffDay2 = dto.OffDay2;
            entity.EffectiveFrom = dto.EffectiveFrom;
            entity.EffectiveTo = dto.EffectiveTo;
            entity.IsActive = dto.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.Set<HrShift>().FirstOrDefaultAsync(s => s.Id == id);
            if (entity == null) return false;

            _context.Set<HrShift>().Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

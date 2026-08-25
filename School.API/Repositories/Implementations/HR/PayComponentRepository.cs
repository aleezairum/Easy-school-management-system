using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.HR;
using School.API.DTOs.HR;
using School.API.Repositories.Interfaces.HR;

namespace School.API.Repositories.Implementations.HR
{
    public class PayComponentRepository : IPayComponentRepository
    {
        private readonly SchoolDbContext _context;

        public PayComponentRepository(SchoolDbContext context)
        {
            _context = context;
        }

        private static PayComponentDto ToDto(HrPayComponent c) => new()
        {
            Id = c.Id,
            Code = c.Code,
            Name = c.Name,
            Kind = (byte)c.Kind,
            CalcType = (byte)c.CalcType,
            Rate = c.Rate,
            RateSource = (byte)c.RateSource,
            BaseOn = c.BaseOn.HasValue ? (byte)c.BaseOn.Value : null,
            DivisorDays = c.DivisorDays,
            DivisorHours = c.DivisorHours,
            GraceMinutes = c.GraceMinutes,
            FreeUnitsPerMonth = c.FreeUnitsPerMonth,
            CapPerDay = c.CapPerDay,
            CapPerMonth = c.CapPerMonth,
            Slabs = c.Slabs.Select(s => new PayComponentSlabDto
            {
                FromValue = s.FromValue,
                ToValue = s.ToValue,
                Amount = s.Amount,
                Days = s.Days,
                Percent = s.Percent
            }).ToList(),
            Formula = c.Formula,
            ScopeType = (byte)c.ScopeType,
            ScopeId = c.ScopeId,
            Priority = c.Priority,
            ShowOnPayslip = c.ShowOnPayslip,
            EffectiveFrom = c.EffectiveFrom,
            EffectiveTo = c.EffectiveTo,
            IsActive = c.IsActive
        };

        private static void ApplyDto(HrPayComponent c, SavePayComponentDto dto)
        {
            c.Code = dto.Code;
            c.Name = dto.Name;
            c.Kind = (HrPayComponentKind)dto.Kind;
            c.CalcType = (HrPayCalcType)dto.CalcType;
            c.Rate = dto.Rate;
            c.RateSource = (HrRateSource)dto.RateSource;
            c.BaseOn = dto.BaseOn.HasValue ? (HrPayBaseOn)dto.BaseOn.Value : null;
            c.DivisorDays = dto.DivisorDays;
            c.DivisorHours = dto.DivisorHours;
            c.GraceMinutes = dto.GraceMinutes;
            c.FreeUnitsPerMonth = dto.FreeUnitsPerMonth;
            c.CapPerDay = dto.CapPerDay;
            c.CapPerMonth = dto.CapPerMonth;
            c.Slabs = dto.Slabs.Select(s => new HrPayComponentSlab
            {
                FromValue = s.FromValue,
                ToValue = s.ToValue,
                Amount = s.Amount,
                Days = s.Days,
                Percent = s.Percent
            }).ToList();
            c.Formula = dto.Formula;
            c.ScopeType = (HrPayScopeType)dto.ScopeType;
            c.ScopeId = dto.ScopeId;
            c.Priority = dto.Priority;
            c.ShowOnPayslip = dto.ShowOnPayslip;
            c.EffectiveFrom = dto.EffectiveFrom;
            c.EffectiveTo = dto.EffectiveTo;
            c.IsActive = dto.IsActive;
        }

        public async Task<List<PayComponentDto>> GetAllAsync()
        {
            var entities = await _context.Set<HrPayComponent>()
                .AsNoTracking()
                .OrderBy(c => c.Kind).ThenBy(c => c.Priority).ThenBy(c => c.Name)
                .ToListAsync();

            return entities.Select(ToDto).ToList();
        }

        public async Task<PayComponentDto?> GetByIdAsync(int id)
        {
            var entity = await _context.Set<HrPayComponent>().AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<HrPayComponent?> GetEntityByIdAsync(int id)
        {
            return await _context.Set<HrPayComponent>().AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<HrPayComponent> CreateAsync(SavePayComponentDto dto)
        {
            var entity = new HrPayComponent();
            ApplyDto(entity, dto);
            _context.Set<HrPayComponent>().Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<HrPayComponent?> UpdateAsync(int id, SavePayComponentDto dto)
        {
            var entity = await _context.Set<HrPayComponent>().FirstOrDefaultAsync(c => c.Id == id);
            if (entity == null) return null;

            ApplyDto(entity, dto);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.Set<HrPayComponent>().FirstOrDefaultAsync(c => c.Id == id);
            if (entity == null) return false;

            _context.Set<HrPayComponent>().Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<HrPayComponent>> GetApplicableAsync(
            DateTime periodStart, DateTime periodEnd, int? designationId, int? hrGradeId, int employeeId)
        {
            var candidates = await _context.Set<HrPayComponent>()
                .AsNoTracking()
                .Where(c => c.IsActive
                            && c.EffectiveFrom <= periodEnd
                            && (c.EffectiveTo == null || c.EffectiveTo >= periodStart)
                            && (c.ScopeType == HrPayScopeType.Global
                                || (c.ScopeType == HrPayScopeType.Employee && c.ScopeId == employeeId)
                                || (c.ScopeType == HrPayScopeType.Designation && designationId != null && c.ScopeId == designationId)
                                || (c.ScopeType == HrPayScopeType.Grade && hrGradeId != null && c.ScopeId == hrGradeId)))
                .ToListAsync();

            // Most-specific-wins per Code: Employee(4) > Designation(3) > Grade(2) > Global(1),
            // Priority as the tie-breaker within the same scope level (section 4.1).
            return candidates
                .GroupBy(c => c.Code)
                .Select(g => g.OrderByDescending(c => (byte)c.ScopeType).ThenByDescending(c => c.Priority).First())
                .ToList();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.HR;
using School.API.DTOs.HR;
using School.API.Repositories.Interfaces.HR;

namespace School.API.Repositories.Implementations.HR
{
    public class EmployeePayRepository : IEmployeePayRepository
    {
        private readonly SchoolDbContext _context;

        public EmployeePayRepository(SchoolDbContext context)
        {
            _context = context;
        }

        private static EmployeePayDto ToDto(HrEmployeePay p) => new()
        {
            Id = p.Id,
            EmployeeId = p.EmployeeId,
            EffectiveFrom = p.EffectiveFrom,
            Basic = p.Basic,
            Components = p.Components.Select(c => new EmployeePayComponentAmountDto
            {
                ComponentId = c.ComponentId,
                Amount = c.Amount
            }).ToList(),
            IsActive = p.IsActive
        };

        public async Task<List<EmployeePayDto>> GetHistoryAsync(int employeeId)
        {
            var rows = await _context.Set<HrEmployeePay>()
                .AsNoTracking()
                .Where(p => p.EmployeeId == employeeId)
                .OrderByDescending(p => p.EffectiveFrom)
                .ToListAsync();

            return rows.Select(ToDto).ToList();
        }

        public async Task<HrEmployeePay> CreateAsync(SaveEmployeePayDto dto)
        {
            var entity = new HrEmployeePay
            {
                EmployeeId = dto.EmployeeId,
                EffectiveFrom = dto.EffectiveFrom.Date,
                Basic = dto.Basic,
                Components = dto.Components.Select(c => new HrPayComponentAmount
                {
                    ComponentId = c.ComponentId,
                    Amount = c.Amount
                }).ToList(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Set<HrEmployeePay>().Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<HrEmployeePay?> GetActiveAsOfAsync(int employeeId, DateTime asOf)
        {
            return await _context.Set<HrEmployeePay>()
                .AsNoTracking()
                .Where(p => p.EmployeeId == employeeId && p.IsActive && p.EffectiveFrom <= asOf.Date)
                .OrderByDescending(p => p.EffectiveFrom)
                .FirstOrDefaultAsync();
        }

        public async Task<Dictionary<int, HrEmployeePay>> GetActiveAsOfForEmployeesAsync(IEnumerable<int> employeeIds, DateTime asOf)
        {
            var idList = employeeIds.Distinct().ToList();
            if (idList.Count == 0) return new Dictionary<int, HrEmployeePay>();

            var rows = await _context.Set<HrEmployeePay>()
                .AsNoTracking()
                .Where(p => idList.Contains(p.EmployeeId) && p.IsActive && p.EffectiveFrom <= asOf.Date)
                .ToListAsync();

            return rows
                .GroupBy(p => p.EmployeeId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.EffectiveFrom).First());
        }
    }
}

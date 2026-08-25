using Microsoft.Extensions.Caching.Memory;
using School.API.DTOs.HR;
using School.API.Repositories.Interfaces.HR;
using School.API.Services.Interfaces.HR;

namespace School.API.Services.Implementations.HR
{
    public class PayComponentService : IPayComponentService
    {
        public const string CacheKey = "hr:paycomponents:all";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        private readonly IPayComponentRepository _repository;
        private readonly IMemoryCache _cache;

        public PayComponentService(IPayComponentRepository repository, IMemoryCache cache)
        {
            _repository = repository;
            _cache = cache;
        }

        public Task<List<PayComponentDto>> GetAllAsync() =>
            _cache.GetOrCreateAsync(CacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                return _repository.GetAllAsync();
            })!;

        public Task<PayComponentDto?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);

        public async Task<PayComponentDto> CreateAsync(SavePayComponentDto dto)
        {
            var entity = await _repository.CreateAsync(dto);
            _cache.Remove(CacheKey);
            return (await _repository.GetByIdAsync(entity.Id))!;
        }

        public async Task<PayComponentDto?> UpdateAsync(int id, SavePayComponentDto dto)
        {
            var entity = await _repository.UpdateAsync(id, dto);
            _cache.Remove(CacheKey);
            return entity == null ? null : await _repository.GetByIdAsync(id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var result = await _repository.DeleteAsync(id);
            _cache.Remove(CacheKey);
            return result;
        }

        public async Task<PayComponentPreviewResultDto?> PreviewAsync(PayComponentPreviewRequestDto dto)
        {
            var entity = await _repository.GetEntityByIdAsync(dto.ComponentId);
            if (entity == null) return null;

            var rule = PayrollCalculator.ToRule(entity);
            var lateDayCount = Math.Max(1, dto.LateCount);
            var attendance = new PayrollCalculator.AttendanceSummary
            {
                // Simulate N late days of the same lateness, so both a single
                // "25 min late" preview and a "3 late days" slab preview work.
                DailyLateMinutes = dto.LateMinutes > 0
                    ? Enumerable.Repeat(dto.LateMinutes, lateDayCount).ToList()
                    : new List<int>(),
                AbsentDays = dto.AbsentDays,
                OvertimeHours = dto.OvertimeHours
            };

            var (amount, explanation) = PayrollCalculator.PreviewSingle(rule, dto.Basic, dto.Gross, attendance, dto.ShiftWorkHours);
            return new PayComponentPreviewResultDto { Amount = amount, Explanation = explanation };
        }
    }
}

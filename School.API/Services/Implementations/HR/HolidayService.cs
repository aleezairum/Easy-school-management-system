using Microsoft.Extensions.Caching.Memory;
using School.API.DTOs.HR;
using School.API.Repositories.Interfaces.HR;
using School.API.Services.Interfaces.HR;

namespace School.API.Services.Implementations.HR
{
    public class HolidayService : IHolidayService
    {
        public const string CacheKeyPrefix = "hr:holidays:";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        private readonly IHolidayRepository _repository;
        private readonly IMemoryCache _cache;

        public HolidayService(IHolidayRepository repository, IMemoryCache cache)
        {
            _repository = repository;
            _cache = cache;
        }

        public Task<List<HolidayDto>> GetAllAsync(int? year = null) =>
            _cache.GetOrCreateAsync(CacheKeyPrefix + (year?.ToString() ?? "all"), entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                return _repository.GetAllAsync(year);
            })!;

        public Task<HolidayDto?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);

        public async Task<HolidayDto> CreateAsync(SaveHolidayDto dto)
        {
            var entity = await _repository.CreateAsync(dto);
            InvalidateCache();
            return (await _repository.GetByIdAsync(entity.Id))!;
        }

        public async Task<HolidayDto?> UpdateAsync(int id, SaveHolidayDto dto)
        {
            var entity = await _repository.UpdateAsync(id, dto);
            InvalidateCache();
            return entity == null ? null : await _repository.GetByIdAsync(id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var result = await _repository.DeleteAsync(id);
            InvalidateCache();
            return result;
        }

        public async Task<int> CopyYearAsync(CopyHolidayYearDto dto)
        {
            var count = await _repository.CopyYearAsync(dto.FromYear, dto.ToYear);
            InvalidateCache();
            return count;
        }

        // Small table, few distinct year keys — clearing "all" plus the two
        // years touched is enough; a full-cache sweep isn't needed here.
        private void InvalidateCache()
        {
            _cache.Remove(CacheKeyPrefix + "all");
        }
    }
}

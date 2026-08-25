using Microsoft.Extensions.Caching.Memory;
using School.API.DTOs.HR;
using School.API.Repositories.Interfaces.HR;
using School.API.Services.Interfaces.HR;

namespace School.API.Services.Implementations.HR
{
    // Shifts change rarely (Setup screen only) — cached per section 9 of the
    // final plan, which flags AddMemoryCache() as registered but unused.
    public class ShiftService : IShiftService
    {
        public const string CacheKey = "hr:shifts:all";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        private readonly IShiftRepository _repository;
        private readonly IMemoryCache _cache;

        public ShiftService(IShiftRepository repository, IMemoryCache cache)
        {
            _repository = repository;
            _cache = cache;
        }

        public Task<List<ShiftDto>> GetAllAsync() =>
            _cache.GetOrCreateAsync(CacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                return _repository.GetAllAsync();
            })!;

        public Task<ShiftDto?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);
        public Task<List<ShiftDropdownDto>> GetDropdownAsync() => _repository.GetDropdownAsync();

        public async Task<ShiftDto> CreateAsync(SaveShiftDto dto)
        {
            var entity = await _repository.CreateAsync(dto);
            _cache.Remove(CacheKey);
            return (await _repository.GetByIdAsync(entity.Id))!;
        }

        public async Task<ShiftDto?> UpdateAsync(int id, SaveShiftDto dto)
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
    }
}

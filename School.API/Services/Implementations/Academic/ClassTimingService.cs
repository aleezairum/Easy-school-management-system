using School.API.DTOs.Academic;
using School.API.DTOs.Common;
using School.API.Repositories.Interfaces.Academic;
using School.API.Services.Interfaces.Academic;

namespace School.API.Services.Implementations.Academic
{
    public class ClassTimingService : IClassTimingService
    {
        private readonly IClassTimingRepository _repo;

        public ClassTimingService(IClassTimingRepository repo)
        {
            _repo = repo;
        }

        public Task<List<ClassTimingDto>> GetAllAsync(int campusId = 0)
            => _repo.GetAllAsync(campusId);

        public Task<ClassTimingDto?> GetByIdAsync(int vid)
            => _repo.GetByIdAsync(vid);

        public Task<ResponseDto?> SaveAsync(SaveClassTimingDto dto, int userId, string userIp)
            => _repo.SaveAsync(dto, userId, userIp);

        public async Task<SaveClassTimingResultDto> SaveBatchAsync(
            SaveClassTimingBatchDto batch, int userId, string userIp)
        {
            var result = new SaveClassTimingResultDto { TotalRequested = batch.Rows.Count };

            foreach (var row in batch.Rows)
            {
                // A class the school has not given a timing to is left alone
                // rather than reported as an error — the grid lists every class,
                // and most schools will leave a few blank.
                //
                // "Blank" means EVERY box, academy included. A row with only
                // academy times filled in is passed to the procedure so the
                // user is told the school times are required, instead of the
                // row disappearing without a word.
                var isBlank = string.IsNullOrWhiteSpace(row.TimeIn)
                           && string.IsNullOrWhiteSpace(row.TimeOut)
                           && string.IsNullOrWhiteSpace(row.AcademyTimeIn)
                           && string.IsNullOrWhiteSpace(row.AcademyTimeOut);

                if (isBlank)
                {
                    result.TotalSkipped++;
                    continue;
                }

                row.CampusID = batch.CampusID;

                var response = await _repo.SaveAsync(row, userId, userIp);

                if (response is null || response.ReturnCode != 0)
                {
                    result.TotalFailed++;
                    result.Errors.Add(
                        $"Class {row.ClassID}: {response?.ReturnMessage ?? "Save failed."}");
                    continue;
                }

                result.TotalSaved++;
            }

            return result;
        }

        public Task<ResponseDto?> DeleteByIdAsync(int vid)
            => _repo.DeleteByIdAsync(vid);
    }
}

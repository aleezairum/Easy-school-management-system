using School.API.Data.DBModels.Academic;
using School.API.DTOs.Academic;
using School.API.Repositories.Interfaces.Academic;
using School.API.Services.Interfaces.Academic;

namespace School.API.Services.Implementations.Academic
{
    public class DailyDiaryService : IDailyDiaryService
    {
        /// <summary>Pages in one save. A diary is a page or two; this stops a mistaken folder upload.</summary>
        private const int MaxImagesPerSave = 10;

        private readonly IDailyDiaryRepository _repository;

        public DailyDiaryService(IDailyDiaryRepository repository)
        {
            _repository = repository;
        }

        public Task<List<DailyDiaryDto>> GetAsync(DateTime? date, int? classId) => _repository.GetAsync(date, classId);

        public async Task<DailyDiaryDto> SaveAsync(SaveDailyDiaryDto dto, int? userId)
        {
            if (dto.DiaryDate == default) throw new InvalidOperationException("Choose the diary date.");
            if (dto.ClassId <= 0) throw new InvalidOperationException("Choose a class.");
            if (dto.Images.Count > MaxImagesPerSave)
                throw new InvalidOperationException($"Upload at most {MaxImagesPerSave} pictures at a time.");

            var sectionId = dto.SectionId is > 0 ? dto.SectionId : null;
            var notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();
            if (notes?.Length > 500) notes = notes[..500];

            // Every picture is checked before anything is saved, so a bad one
            // does not leave half a diary behind.
            var decoded = dto.Images.Select(UploadedImage.Decode).ToList();

            var diary = await _repository.FindAsync(dto.DiaryDate, dto.ClassId, sectionId);
            if (diary == null)
            {
                if (decoded.Count == 0 && notes == null)
                    throw new InvalidOperationException("Add at least one picture of the diary.");
                diary = new SMSDailyDiary
                {
                    DiaryDate = dto.DiaryDate.Date,
                    ClassID = dto.ClassId,
                    SectionID = sectionId,
                    Notes = notes,
                    InsertedBy = userId,
                    InsertedDate = DateTime.Now
                };
                diary = await _repository.AddAsync(diary);
            }
            else
            {
                if (decoded.Count == 0 && notes == null)
                    throw new InvalidOperationException("Nothing to add: choose pictures or write a note.");
                // A note typed now replaces the old one; leaving the box empty keeps it.
                if (notes != null) diary.Notes = notes;
                diary.UpdatedBy = userId;
                diary.UpdatedDate = DateTime.Now;
            }

            var next = diary.Images.Count == 0 ? 1 : diary.Images.Max(i => i.SortOrder) + 1;
            foreach (var (data, type, name) in decoded)
            {
                diary.Images.Add(new SMSDailyDiaryImage
                {
                    SortOrder = next++,
                    FileName = name,
                    ContentType = type,
                    ImageData = data,
                    InsertedBy = userId,
                    InsertedDate = DateTime.Now
                });
            }
            await _repository.SaveChangesAsync();

            return (await _repository.GetAsync(diary.DiaryDate, diary.ClassID)).First(d => d.Id == diary.VID);
        }

        public Task<SMSDailyDiaryImage?> GetImageAsync(int imageId) => _repository.GetImageAsync(imageId);
        public Task<bool> DeleteImageAsync(int imageId) => _repository.DeleteImageAsync(imageId);
        public Task<bool> DeleteAsync(int diaryId) => _repository.DeleteAsync(diaryId);
    }
}

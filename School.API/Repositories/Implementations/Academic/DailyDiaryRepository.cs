using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.Academic;
using School.API.DTOs.Academic;
using School.API.Repositories.Interfaces.Academic;

namespace School.API.Repositories.Implementations.Academic
{
    public class DailyDiaryRepository : IDailyDiaryRepository
    {
        private readonly SchoolDbContext _context;

        public DailyDiaryRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<List<DailyDiaryDto>> GetAsync(DateTime? date, int? classId)
        {
            var d = date?.Date;

            // The image bytes are never read here - only their size - so a
            // list of a week's diaries does not pull megabytes of pictures.
            var rows = await (
                from x in _context.SMSDailyDiaries.AsNoTracking()
                join c in _context.SMSClasses.AsNoTracking() on x.ClassID equals c.VID into cj
                from c in cj.DefaultIfEmpty()
                join s in _context.SMSSections.AsNoTracking() on x.SectionID equals s.VID into sj
                from s in sj.DefaultIfEmpty()
                where (!d.HasValue || x.DiaryDate == d) && (!classId.HasValue || x.ClassID == classId)
                select new DailyDiaryDto
                {
                    Id = x.VID,
                    DiaryDate = x.DiaryDate,
                    ClassId = x.ClassID,
                    ClassName = c != null ? c.VName : null,
                    SectionId = x.SectionID,
                    SectionName = s != null ? s.VName : null,
                    Notes = x.Notes,
                    Images = x.Images.OrderBy(i => i.SortOrder).ThenBy(i => i.VID)
                        .Select(i => new DailyDiaryImageDto { Id = i.VID, FileName = i.FileName, SizeBytes = i.ImageData.Length })
                        .ToList()
                }).ToListAsync();

            return rows.OrderByDescending(r => r.DiaryDate).ThenBy(r => r.ClassName).ThenBy(r => r.SectionName).ToList();
        }

        public Task<SMSDailyDiary?> FindAsync(DateTime date, int classId, int? sectionId) =>
            _context.SMSDailyDiaries.Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.DiaryDate == date.Date && x.ClassID == classId && x.SectionID == sectionId);

        public async Task<SMSDailyDiary> AddAsync(SMSDailyDiary diary)
        {
            _context.SMSDailyDiaries.Add(diary);
            await _context.SaveChangesAsync();
            return diary;
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();

        public Task<SMSDailyDiaryImage?> GetImageAsync(int imageId) =>
            _context.SMSDailyDiaryImages.AsNoTracking().FirstOrDefaultAsync(i => i.VID == imageId);

        public async Task<bool> DeleteImageAsync(int imageId)
        {
            var image = await _context.SMSDailyDiaryImages.FirstOrDefaultAsync(i => i.VID == imageId);
            if (image == null) return false;

            _context.SMSDailyDiaryImages.Remove(image);
            var diary = await _context.SMSDailyDiaries.FirstOrDefaultAsync(x => x.VID == image.DiaryID);
            var othersLeft = await _context.SMSDailyDiaryImages.AnyAsync(i => i.DiaryID == image.DiaryID && i.VID != imageId);
            if (diary != null && !othersLeft && string.IsNullOrWhiteSpace(diary.Notes))
                _context.SMSDailyDiaries.Remove(diary);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int diaryId)
        {
            // Images go with it (ON DELETE CASCADE in the table).
            return await _context.SMSDailyDiaries.Where(x => x.VID == diaryId).ExecuteDeleteAsync() > 0;
        }
    }
}

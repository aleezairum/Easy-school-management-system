using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.SMS;
using School.API.DTOs.SMS;
using School.API.Repositories.Interfaces;

namespace School.API.Repositories.Implementations
{
    public class AnnouncementRepository : IAnnouncementRepository
    {
        private readonly SchoolDbContext _context;

        public AnnouncementRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<List<AnnouncementDto>> GetAllAsync()
        {
            var today = DateTime.Today;
            return await (
                from a in _context.SMSAnnouncements.AsNoTracking()
                join c in _context.Campuses.AsNoTracking() on a.CampusID equals c.VID into cj
                from c in cj.DefaultIfEmpty()
                orderby a.StartDate descending, a.VID descending
                select new AnnouncementDto
                {
                    Id = a.VID,
                    Title = a.Title,
                    Message = a.Message,
                    StartDate = a.StartDate,
                    EndDate = a.EndDate,
                    Audience = a.Audience,
                    CampusId = a.CampusID,
                    CampusName = c != null ? c.VName : null,
                    HasImage = a.ImageData != null,
                    ImageFileName = a.ImageFileName,
                    IsActive = a.IsActive,
                    IsCurrent = a.IsActive && a.StartDate <= today && (a.EndDate == null || a.EndDate >= today)
                }).ToListAsync();
        }

        public Task<SMSAnnouncement?> FindAsync(int id) =>
            _context.SMSAnnouncements.FirstOrDefaultAsync(a => a.VID == id);

        public async Task<SMSAnnouncement> AddAsync(SMSAnnouncement entity)
        {
            _context.SMSAnnouncements.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();

        public async Task<bool> DeleteAsync(int id) =>
            await _context.SMSAnnouncements.Where(a => a.VID == id).ExecuteDeleteAsync() > 0;
    }
}

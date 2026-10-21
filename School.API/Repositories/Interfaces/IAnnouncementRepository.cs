using School.API.Data.DBModels.SMS;
using School.API.DTOs.SMS;

namespace School.API.Repositories.Interfaces
{
    public interface IAnnouncementRepository
    {
        /// <summary>Every announcement, newest first, without the image bytes.</summary>
        Task<List<AnnouncementDto>> GetAllAsync();

        /// <summary>Tracked, image included.</summary>
        Task<SMSAnnouncement?> FindAsync(int id);

        Task<SMSAnnouncement> AddAsync(SMSAnnouncement entity);
        Task SaveChangesAsync();
        Task<bool> DeleteAsync(int id);
    }
}

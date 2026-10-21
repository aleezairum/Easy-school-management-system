using School.API.Data.DBModels.SMS;
using School.API.DTOs.SMS;

namespace School.API.Services.Interfaces
{
    public interface IAnnouncementService
    {
        Task<List<AnnouncementDto>> GetAllAsync();
        Task<SMSAnnouncement?> FindAsync(int id);

        /// <exception cref="InvalidOperationException">Missing title or message, bad dates or a bad image.</exception>
        Task<AnnouncementDto> SaveAsync(int? id, SaveAnnouncementDto dto, int? userId);

        Task<bool> DeleteAsync(int id);
    }
}

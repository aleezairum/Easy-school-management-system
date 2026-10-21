using School.API.Data.DBModels.SMS;
using School.API.DTOs.SMS;
using School.API.Repositories.Interfaces;
using School.API.Services.Implementations.Academic;
using School.API.Services.Implementations.MobileApp;
using School.API.Services.Interfaces;

namespace School.API.Services.Implementations
{
    public class AnnouncementService : IAnnouncementService
    {
        private readonly IAnnouncementRepository _repository;
        private readonly AppPushSignal _push;

        public AnnouncementService(IAnnouncementRepository repository, AppPushSignal push)
        {
            _repository = repository;
            _push = push;
        }

        public Task<List<AnnouncementDto>> GetAllAsync() => _repository.GetAllAsync();
        public Task<SMSAnnouncement?> FindAsync(int id) => _repository.FindAsync(id);

        public async Task<AnnouncementDto> SaveAsync(int? id, SaveAnnouncementDto dto, int? userId)
        {
            var title = dto.Title?.Trim() ?? "";
            var message = dto.Message?.Trim() ?? "";
            if (title.Length == 0) throw new InvalidOperationException("Write a title.");
            if (title.Length > 200) throw new InvalidOperationException("Keep the title within 200 characters.");
            if (message.Length == 0) throw new InvalidOperationException("Write the announcement.");
            // WhatsApp refuses a message over 4096 characters; leave room for the heading.
            if (message.Length > 3500) throw new InvalidOperationException("Keep the announcement within 3,500 characters - WhatsApp will not send a longer one.");
            if (dto.StartDate == default) throw new InvalidOperationException("Choose the date it starts.");
            if (dto.EndDate.HasValue && dto.EndDate.Value.Date < dto.StartDate.Date)
                throw new InvalidOperationException("The end date is before the start date.");
            if (dto.Audience > 2) throw new InvalidOperationException("Choose who it is for.");

            var image = dto.Image != null && !string.IsNullOrWhiteSpace(dto.Image.Base64)
                ? UploadedImage.Decode(dto.Image) : ((byte[], string, string)?)null;

            SMSAnnouncement entity;
            if (id is > 0)
            {
                entity = await _repository.FindAsync(id.Value)
                         ?? throw new KeyNotFoundException();
                entity.UpdatedBy = userId;
                entity.UpdatedDate = DateTime.Now;
            }
            else
            {
                entity = new SMSAnnouncement { InsertedBy = userId, InsertedDate = DateTime.Now };
            }

            entity.Title = title;
            entity.Message = message;
            entity.StartDate = dto.StartDate.Date;
            entity.EndDate = dto.EndDate?.Date;
            entity.Audience = dto.Audience;
            entity.CampusID = dto.CampusId is > 0 ? dto.CampusId : null;
            entity.IsActive = dto.IsActive;

            if (image is { } img)
            {
                entity.ImageData = img.Item1;
                entity.ImageContentType = img.Item2;
                entity.ImageFileName = img.Item3;
            }
            else if (dto.RemoveImage)
            {
                entity.ImageData = null;
                entity.ImageContentType = null;
                entity.ImageFileName = null;
            }

            if (entity.VID == 0) entity = await _repository.AddAsync(entity);
            else await _repository.SaveChangesAsync();
            // The mobile app's push job looks at once rather than within the minute.
            _push.Poke();

            return (await _repository.GetAllAsync()).First(a => a.Id == entity.VID);
        }

        public Task<bool> DeleteAsync(int id) => _repository.DeleteAsync(id);
    }
}

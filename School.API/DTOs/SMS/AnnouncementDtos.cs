using School.API.DTOs.Academic;

namespace School.API.DTOs.SMS
{
    public class AnnouncementDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        /// <summary>0 everyone, 1 parents only, 2 staff only.</summary>
        public byte Audience { get; set; }
        public int? CampusId { get; set; }
        public string? CampusName { get; set; }
        public bool HasImage { get; set; }
        public string? ImageFileName { get; set; }
        public bool IsActive { get; set; }

        /// <summary>Being given out on WhatsApp today.</summary>
        public bool IsCurrent { get; set; }
    }

    public class SaveAnnouncementDto
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public byte Audience { get; set; }
        public int? CampusId { get; set; }
        public bool IsActive { get; set; } = true;

        /// <summary>A new image; null keeps the one there is.</summary>
        public UploadImageDto? Image { get; set; }

        /// <summary>Take the image off.</summary>
        public bool RemoveImage { get; set; }
    }
}

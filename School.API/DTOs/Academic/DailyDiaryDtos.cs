namespace School.API.DTOs.Academic
{
    public class DailyDiaryDto
    {
        public int Id { get; set; }
        public DateTime DiaryDate { get; set; }
        public int ClassId { get; set; }
        public string? ClassName { get; set; }

        /// <summary>Null: every section of the class.</summary>
        public int? SectionId { get; set; }
        public string? SectionName { get; set; }
        public string? Notes { get; set; }
        public List<DailyDiaryImageDto> Images { get; set; } = new();
    }

    /// <summary>An image's details - the picture itself is fetched by id.</summary>
    public class DailyDiaryImageDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public int SizeBytes { get; set; }
    }

    /// <summary>
    /// Saves a diary. The date, class and section pick the diary - made if
    /// missing - and the images are ADDED to it, so a second upload for the
    /// same class and day adds pages rather than replacing them.
    /// </summary>
    public class SaveDailyDiaryDto
    {
        public DateTime DiaryDate { get; set; }
        public int ClassId { get; set; }
        public int? SectionId { get; set; }
        public string? Notes { get; set; }
        public List<UploadImageDto> Images { get; set; } = new();
    }

    /// <summary>An image sent from the browser, base64-encoded.</summary>
    public class UploadImageDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "image/jpeg";
        public string Base64 { get; set; } = string.Empty;
    }
}

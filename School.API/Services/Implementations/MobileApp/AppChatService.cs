using Microsoft.Extensions.Caching.Memory;
using School.API.DTOs.MobileApp;
using School.API.DTOs.SMS;
using School.API.Repositories.Implementations.MobileApp;
using School.API.Services.Interfaces;

namespace School.API.Services.Implementations.MobileApp
{
    /// <summary>
    /// The app's "Chat" tab: the school's WhatsApp self-service menu inside the
    /// app. Every answer is composed by WhatsAppBotService itself
    /// (ComposeReplyForAsync), so the menu, the wording and the PDFs are the
    /// same as on WhatsApp - but nothing goes through Meta, so it costs nothing
    /// and works while the WhatsApp auto-reply is switched off.
    ///
    /// Who is asking comes from the sign-in, not a phone number:
    ///   parent  - every child of the family (parents' menu);
    ///   student - only themself;
    ///   staff   - the staff menu, plus the parents' menu for their own children.
    ///
    /// PDFs and pictures are held for 30 minutes for the phone to download
    /// (GET api/app/chat/files/{id}); the phone keeps its own copy after that.
    /// </summary>
    public class AppChatService
    {
        private static readonly TimeSpan FileLife = TimeSpan.FromMinutes(30);
        private const int MaxText = 500;

        private readonly AppService _app;
        private readonly AppRepository _repo;
        private readonly IWhatsAppBotService _bot;
        private readonly IMemoryCache _cache;

        public AppChatService(AppService app, AppRepository repo, IWhatsAppBotService bot, IMemoryCache cache)
        {
            _app = app; _repo = repo; _bot = bot; _cache = cache;
        }

        // Per account AND mode: a parent's family voucher is not the student-mode login's to fetch.
        private static string Key(AppCaller c, string fileId) => $"appchat:{c.AccountId}:{c.Mode}:{fileId}";

        public async Task<List<AppChatMessageDto>> SendAsync(AppCaller c, string? text)
        {
            text = (text ?? "").Trim();
            if (text.Length > MaxText) text = text[..MaxText];

            var students = (await _app.VisibleStudentsAsync(c))
                .Select(s => AppService.Bot(s, c.Mode == "parent" ? c.ParentId : null))
                .ToList();
            var staff = new List<BotStaffDto>();
            if (c.IsStaff && c.EmployeeId.HasValue && (await _repo.GetStaffAsync(c.EmployeeId.Value)) is { } me)
                staff.Add(AppService.BotStaff(me.Card));

            var replies = await _bot.ComposeReplyForAsync(students, staff, text);

            var result = new List<AppChatMessageDto>();
            foreach (var r in replies)
            {
                var m = new AppChatMessageDto { Text = r.Text };
                if (r.Interactive is { } list)
                {
                    m.Text = list.Body;
                    m.Menu = new AppChatMenuDto
                    {
                        Header = list.Header,
                        Footer = list.Footer,
                        ButtonText = list.ButtonText,
                        Rows = list.Rows.Select(x => new AppChatMenuRowDto { Id = x.Id, Title = x.Title, Description = x.Description }).ToList()
                    };
                }
                if (r.HasDocument)
                {
                    var name = r.DocumentFileName ?? "document.pdf";
                    var ext = Path.GetExtension(name).ToLowerInvariant();
                    var kind = ext is ".jpg" or ".jpeg" or ".png" or ".webp" ? "image" : "pdf";
                    var type = kind == "pdf" ? "application/pdf" : (ext == ".png" ? "image/png" : ext == ".webp" ? "image/webp" : "image/jpeg");
                    var id = Guid.NewGuid().ToString("N");
                    _cache.Set(Key(c, id), (r.DocumentBytes!, type, name), FileLife);
                    m.File = new AppChatFileDto { Id = id, Name = name, Kind = kind, Size = r.DocumentBytes!.Length };
                }
                result.Add(m);
            }
            return result;
        }

        /// <summary>A PDF or picture from a reply - only to the account it was made for.</summary>
        public (byte[] Data, string Type, string Name)? File(AppCaller c, string id) =>
            _cache.TryGetValue(Key(c, id), out (byte[], string, string) f) ? f : null;
    }
}

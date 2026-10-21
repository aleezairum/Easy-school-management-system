// Services/Interfaces/IWhatsAppBotService.cs
namespace School.API.Services.Interfaces
{
    /// <summary>
    /// One outgoing WhatsApp message from the auto-reply: text, or a PDF with
    /// a caption.
    /// </summary>
    public class BotOutgoing
    {
        /// <summary>Message body, or the document's caption when a PDF is attached.</summary>
        public string Text { get; set; } = string.Empty;

        public byte[]? DocumentBytes { get; set; }
        public string? DocumentFileName { get; set; }

        /// <summary>
        /// A tappable menu instead of plain text. <see cref="Text"/> is kept as
        /// the fallback wording for senders that cannot render one.
        /// </summary>
        public SmsInteractiveList? Interactive { get; set; }

        public bool HasDocument => DocumentBytes is { Length: > 0 };

        public static BotOutgoing Message(string text) => new() { Text = text };

        public static BotOutgoing Document(string caption, byte[] pdf, string fileName) =>
            new() { Text = caption, DocumentBytes = pdf, DocumentFileName = fileName };

        /// <summary>
        /// A picture with a caption. Carried like a document; the .jpg / .png
        /// name is what makes the sender post it as an image, shown in the chat
        /// rather than as a file to download.
        /// </summary>
        public static BotOutgoing Image(string caption, byte[] image, string fileName) =>
            new() { Text = caption, DocumentBytes = image, DocumentFileName = fileName };

        /// <param name="text">Shown verbatim if the interactive send is refused.</param>
        public static BotOutgoing Menu(string text, SmsInteractiveList list) =>
            new() { Text = text, Interactive = list };
    }

    /// <summary>
    /// The school's documents as PDF - the same ones the WhatsApp menu and the
    /// print buttons produce - for the mobile app. Works whether or not the
    /// WhatsApp auto-reply is switched on.
    /// </summary>
    public interface ISchoolDocuments
    {
        /// <summary>The latest fee voucher(s) of these children (one per distinct voucher).</summary>
        Task<List<BotOutgoing>> VoucherPdfsAsync(List<School.API.DTOs.SMS.BotStudentDto> students);

        /// <summary>A family ledger for children of one parent, otherwise each child's own ledger.</summary>
        Task<List<BotOutgoing>> LedgerPdfsAsync(List<School.API.DTOs.SMS.BotStudentDto> students);

        /// <summary>The child's result card for one exam (null when there is none).</summary>
        Task<BotOutgoing?> ResultCardPdfAsync(School.API.DTOs.SMS.BotStudentDto student, int examId, string examName);

        /// <summary>A member of staff's attendance card for a month; Note says why there is none.</summary>
        Task<(BotOutgoing? Doc, string? Note)> AttendanceCardPdfAsync(School.API.DTOs.SMS.BotStaffDto staff, DateTime month);

        /// <summary>A member of staff's salary slip for a month; Note says why there is none.</summary>
        Task<(BotOutgoing? Doc, string? Note)> SalarySlipPdfAsync(School.API.DTOs.SMS.BotStaffDto staff, DateTime month);
    }

    public interface IWhatsAppBotService
    {
        /// <summary>
        /// What to send back for an incoming WhatsApp message — empty to stay silent.
        /// </summary>
        /// <remarks>
        /// A list, not a single message, because one request can produce several:
        /// a family with two children asking for their result cards gets one PDF
        /// per child. WhatsApp carries a single document per message, so they
        /// cannot be combined.
        ///
        /// Composing and sending are kept apart on purpose: sending already
        /// lives in WhatsAppChatService.SendReplyAsync, which enforces the
        /// 24-hour window and records the outbound row so an automatic answer
        /// shows in the chat thread exactly like a staff reply.
        /// </remarks>
        Task<List<BotOutgoing>> ComposeReplyAsync(string waId, string? incomingText);

        /// <summary>
        /// The same reply for a sender already known (the mobile app's chat):
        /// these children get the parents' menu, these staff the staff menu.
        /// Works while the WhatsApp auto-reply is switched off.
        /// </summary>
        Task<List<BotOutgoing>> ComposeReplyForAsync(List<School.API.DTOs.SMS.BotStudentDto> students,
            List<School.API.DTOs.SMS.BotStaffDto> staff, string? incomingText);
    }
}

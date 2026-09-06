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

        public bool HasDocument => DocumentBytes is { Length: > 0 };

        public static BotOutgoing Message(string text) => new() { Text = text };

        public static BotOutgoing Document(string caption, byte[] pdf, string fileName) =>
            new() { Text = caption, DocumentBytes = pdf, DocumentFileName = fileName };
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
    }
}

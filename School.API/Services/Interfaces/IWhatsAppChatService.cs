using School.API.DTOs;

namespace School.API.Services.Interfaces
{
    public interface IWhatsAppChatService
    {
        // Parses a Meta webhook payload: stores incoming messages and applies
        // delivery statuses to outbound chat rows. Returns how many rows the
        // payload produced or updated.
        Task<int> IngestWebhookAsync(string rawJson);

        Task<List<WaConversationDto>> GetConversationsAsync(string? search, int take);

        Task<WaThreadDto?> GetThreadAsync(string waId, int? beforeVid, int take);

        /// <summary>
        /// Free-form reply, only permitted while the contact's 24-hour service
        /// window is open. Pass document bytes to send a PDF with
        /// <paramref name="text"/> as its caption — that is how the auto-reply
        /// delivers the fee voucher and result card.
        /// </summary>
        Task<(bool ok, string? error, WaMessageDto? message)> SendReplyAsync(
            string waId, string text,
            byte[]? documentBytes = null, string? documentFileName = null);

        Task<int> MarkReadAsync(string waId);

        Task<int> GetTotalUnreadAsync();

        // Records an outbound campaign message in the contact's thread so the
        // chat shows the complete history, not just replies.
        Task MirrorOutboundAsync(
            string phoneNumber, string? body, string? messageId,
            string? templateName, string? status, int? smsMessageVid);
    }
}

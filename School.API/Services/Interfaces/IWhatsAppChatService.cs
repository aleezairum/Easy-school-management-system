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

        // Sends a free-form text reply. Only permitted while the contact's
        // 24-hour service window is open.
        Task<(bool ok, string? error, WaMessageDto? message)> SendReplyAsync(
            string waId, string text);

        Task<int> MarkReadAsync(string waId);

        Task<int> GetTotalUnreadAsync();

        // Records an outbound campaign message in the contact's thread so the
        // chat shows the complete history, not just replies.
        Task MirrorOutboundAsync(
            string phoneNumber, string? body, string? messageId,
            string? templateName, string? status, int? smsMessageVid);
    }
}

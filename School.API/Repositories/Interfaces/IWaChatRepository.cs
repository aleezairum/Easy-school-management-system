using School.API.Data.DBModels.SMS;

namespace School.API.Repositories.Interfaces
{
    public interface IWaChatRepository
    {
        // Finds the thread for a contact, creating it on first contact. The
        // display name is resolved from our own student/parent records once,
        // when the thread is created.
        Task<WaConversation> GetOrCreateConversationAsync(string waId, string? profileName);

        Task<WaConversation?> GetConversationAsync(string waId);
        Task<WaConversation?> GetConversationByIdAsync(int conversationId);

        // Chat list, most recent activity first.
        Task<List<WaConversation>> GetConversationsAsync(string? search, int take);

        // Stores an inbound message. Returns false when Meta re-delivered a
        // wamid we already have (webhook retries are expected and normal).
        Task<bool> AddInboundAsync(WaMessage message);

        Task AddOutboundAsync(WaMessage message);

        // One page of a thread, oldest-first, ending at beforeVid when paging back.
        Task<List<WaMessage>> GetMessagesAsync(int conversationId, int? beforeVid, int take);

        // Marks the thread's inbound messages as opened by school staff.
        Task<int> MarkReadAsync(int conversationId);

        // Applies a delivery status webhook to an outbound chat message.
        Task<bool> ApplyStatusAsync(
            string messageId, string status, DateTime timestamp,
            string? errorCode, string? errorMessage);

        Task<int> GetTotalUnreadAsync();
    }
}

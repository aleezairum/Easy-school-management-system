using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.SMS;
using School.API.Repositories.Interfaces;

namespace School.API.Repositories.Implementations
{
    public class WaChatRepository : IWaChatRepository
    {
        private readonly SchoolDbContext _context;
        private readonly ILogger<WaChatRepository> _logger;

        public WaChatRepository(SchoolDbContext context, ILogger<WaChatRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<WaConversation> GetOrCreateConversationAsync(string waId, string? profileName)
        {
            var conversation = await _context.WaConversations
                .FirstOrDefaultAsync(c => c.WaId == waId);

            if (conversation != null)
            {
                // Contacts can rename their WhatsApp profile at any time.
                if (!string.IsNullOrWhiteSpace(profileName) &&
                    conversation.ProfileName != profileName)
                {
                    conversation.ProfileName = profileName;
                    conversation.UpdatedDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
                return conversation;
            }

            conversation = new WaConversation
            {
                WaId = waId,
                ProfileName = profileName,
                // Resolved once, on first contact — the chat list should show a
                // student's name rather than a bare number wherever we know it.
                ContactName = await ResolveContactNameAsync(waId),
                InsertedDate = DateTime.UtcNow
            };

            _context.WaConversations.Add(conversation);
            await _context.SaveChangesAsync();
            return conversation;
        }

        public Task<WaConversation?> GetConversationAsync(string waId)
            => _context.WaConversations.FirstOrDefaultAsync(c => c.WaId == waId);

        public Task<WaConversation?> GetConversationByIdAsync(int conversationId)
            => _context.WaConversations.FirstOrDefaultAsync(c => c.VID == conversationId);

        public async Task<List<WaConversation>> GetConversationsAsync(string? search, int take)
        {
            var query = _context.WaConversations.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(c =>
                    c.WaId.Contains(term) ||
                    (c.ContactName != null && c.ContactName.Contains(term)) ||
                    (c.ProfileName != null && c.ProfileName.Contains(term)));
            }

            return await query
                .OrderByDescending(c => c.LastMessageAt ?? c.InsertedDate)
                .Take(take < 1 ? 100 : take)
                .ToListAsync();
        }

        public async Task<bool> AddInboundAsync(WaMessage message)
        {
            // Meta retries a webhook whenever it is unsure we received it, so
            // the same wamid can arrive several times. Skip duplicates.
            if (!string.IsNullOrEmpty(message.MessageId) &&
                await _context.WaMessages.AnyAsync(m => m.MessageId == message.MessageId))
            {
                return false;
            }

            message.Direction = WaDirection.Inbound;
            _context.WaMessages.Add(message);

            var conversation = await _context.WaConversations
                .FirstOrDefaultAsync(c => c.VID == message.ConversationId);

            if (conversation != null)
            {
                conversation.LastMessageAt = message.MessageTime;
                conversation.LastMessagePreview = Preview(message);
                conversation.LastMessageDirection = WaDirection.Inbound;
                // The 24-hour reply window restarts on every inbound message.
                conversation.LastInboundAt = message.MessageTime;
                conversation.UnreadCount += 1;
                conversation.UpdatedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task AddOutboundAsync(WaMessage message)
        {
            message.Direction = WaDirection.Outbound;
            _context.WaMessages.Add(message);

            var conversation = await _context.WaConversations
                .FirstOrDefaultAsync(c => c.VID == message.ConversationId);

            if (conversation != null)
            {
                conversation.LastMessageAt = message.MessageTime;
                conversation.LastMessagePreview = Preview(message);
                conversation.LastMessageDirection = WaDirection.Outbound;
                conversation.UpdatedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<WaMessage>> GetMessagesAsync(int conversationId, int? beforeVid, int take)
        {
            if (take < 1 || take > 200) take = 50;

            var query = _context.WaMessages.AsNoTracking()
                .Where(m => m.ConversationId == conversationId);

            if (beforeVid.HasValue)
                query = query.Where(m => m.VID < beforeVid.Value);

            // Newest page is fetched, then flipped so the UI renders oldest-first.
            var page = await query
                .OrderByDescending(m => m.VID)
                .Take(take)
                .ToListAsync();

            page.Reverse();
            return page;
        }

        public async Task<int> MarkReadAsync(int conversationId)
        {
            var unread = await _context.WaMessages
                .Where(m => m.ConversationId == conversationId &&
                            m.Direction == WaDirection.Inbound &&
                            !m.IsRead)
                .ToListAsync();

            foreach (var message in unread)
            {
                message.IsRead = true;
                message.UpdatedDate = DateTime.UtcNow;
            }

            var conversation = await _context.WaConversations
                .FirstOrDefaultAsync(c => c.VID == conversationId);
            if (conversation != null)
            {
                conversation.UnreadCount = 0;
                conversation.UpdatedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return unread.Count;
        }

        public async Task<bool> ApplyStatusAsync(
            string messageId, string status, DateTime timestamp,
            string? errorCode, string? errorMessage)
        {
            if (string.IsNullOrWhiteSpace(messageId)) return false;

            var message = await _context.WaMessages
                .FirstOrDefaultAsync(m => m.MessageId == messageId);
            if (message == null) return false;

            message.Status = status;

            switch (status)
            {
                case "delivered":
                    message.DeliveredAt = timestamp;
                    break;
                case "read":
                    message.ReadAt = timestamp;
                    message.DeliveredAt ??= timestamp;
                    break;
                case "failed":
                    message.ErrorCode = Truncate(errorCode, 50) ?? message.ErrorCode;
                    message.ErrorMessage = Truncate(errorMessage, 500) ?? message.ErrorMessage;
                    break;
            }

            message.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetTotalUnreadAsync()
            => await _context.WaConversations.SumAsync(c => (int?)c.UnreadCount) ?? 0;

        // ─────────────────────────────────────────────────────────────
        // Name resolution — match the contact against our own records.
        // Numbers are stored locally as "03001234567" but WhatsApp ids are
        // "923001234567", so both sides are compared on their last 10 digits.
        // ─────────────────────────────────────────────────────────────
        private async Task<string?> ResolveContactNameAsync(string waId)
        {
            var digits = new string(waId.Where(char.IsDigit).ToArray());
            if (digits.Length < 10) return null;
            var last10 = digits.Substring(digits.Length - 10);

            const string sql = @"
                SELECT TOP 1 X.Name
                FROM (
                    SELECT ST.Name, ST.MobileNo AS Phone
                      FROM SMSStudent ST
                     WHERE ST.MobileNo IS NOT NULL
                    UNION ALL
                    SELECT ST.Name, PA.FatherMobile
                      FROM SMSStudent ST
                     INNER JOIN SMSParent PA ON PA.VID = ST.ParentID
                     WHERE PA.FatherMobile IS NOT NULL
                    UNION ALL
                    SELECT ST.Name, PA.MotherMobile
                      FROM SMSStudent ST
                     INNER JOIN SMSParent PA ON PA.VID = ST.ParentID
                     WHERE PA.MotherMobile IS NOT NULL
                    UNION ALL
                    SELECT ST.Name, PA.GuardianMobile
                      FROM SMSStudent ST
                     INNER JOIN SMSParent PA ON PA.VID = ST.ParentID
                     WHERE PA.GuardianMobile IS NOT NULL
                ) X
                WHERE RIGHT(REPLACE(REPLACE(REPLACE(X.Phone, ' ', ''), '-', ''), '+', ''), 10) = @Last10;";

            try
            {
                var connectionString = _context.Database.GetConnectionString();
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                await using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Last10", last10);

                var result = await command.ExecuteScalarAsync();
                return result as string;
            }
            catch (Exception ex)
            {
                // A name is a nicety — never let lookup failure block a message.
                _logger.LogWarning(ex, "[WA] Could not resolve contact name for {WaId}", waId);
                return null;
            }
        }

        private static string Preview(WaMessage message)
        {
            var text = message.MessageKind == "text"
                ? message.Body ?? string.Empty
                : $"[{message.MessageKind}]" +
                  (string.IsNullOrWhiteSpace(message.Body) ? "" : $" {message.Body}");

            text = text.Replace('\n', ' ').Replace('\r', ' ').Trim();
            return text.Length <= 500 ? text : text.Substring(0, 500);
        }

        private static string? Truncate(string? value, int max)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= max ? value : value.Substring(0, max);
        }
    }
}

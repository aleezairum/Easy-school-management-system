using System.Text.Json;
using School.API.Data.DBModels.SMS;
using School.API.DTOs;
using School.API.Repositories.Interfaces;
using School.API.Services.Interfaces;

namespace School.API.Services.Implementations
{
    // Two-way WhatsApp chat. Incoming messages exist only because we store the
    // webhook events Meta pushes — there is no API to fetch history — so this
    // service is the single point where inbound traffic enters the system.
    public class WhatsAppChatService : IWhatsAppChatService
    {
        // Meta's customer service window: free-form replies are allowed for 24
        // hours after the contact's most recent inbound message.
        private static readonly TimeSpan ServiceWindow = TimeSpan.FromHours(24);

        private readonly IWaChatRepository _repository;
        private readonly ISmsSender _sender;
        private readonly ILogger<WhatsAppChatService> _logger;
        private readonly string _countryCode;

        public WhatsAppChatService(
            IWaChatRepository repository,
            ISmsSender sender,
            IConfiguration configuration,
            ILogger<WhatsAppChatService> logger)
        {
            _repository = repository;
            _sender = sender;
            _logger = logger;
            _countryCode = configuration["MetaWhatsApp:CountryCode"] ?? "92";
        }

        // ─────────────────────────────────────────────────────────────
        // Webhook ingestion
        //
        //   entry[].changes[].value.contacts[] = { wa_id, profile: { name } }
        //   entry[].changes[].value.messages[] = { id, from, timestamp, type,
        //                                          text: { body }, ... }
        //   entry[].changes[].value.statuses[] = { id, status, timestamp }
        // ─────────────────────────────────────────────────────────────
        public async Task<int> IngestWebhookAsync(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson)) return 0;

            int affected = 0;

            try
            {
                using var doc = JsonDocument.Parse(rawJson);

                if (!doc.RootElement.TryGetProperty("entry", out var entries) ||
                    entries.ValueKind != JsonValueKind.Array)
                    return 0;

                foreach (var entry in entries.EnumerateArray())
                {
                    if (!entry.TryGetProperty("changes", out var changes) ||
                        changes.ValueKind != JsonValueKind.Array)
                        continue;

                    foreach (var change in changes.EnumerateArray())
                    {
                        if (!change.TryGetProperty("value", out var value)) continue;

                        affected += await IngestMessagesAsync(value);
                        affected += await IngestStatusesAsync(value);
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "[WA] Unparseable webhook payload");
                return 0;
            }

            return affected;
        }

        private async Task<int> IngestMessagesAsync(JsonElement value)
        {
            if (!value.TryGetProperty("messages", out var messages) ||
                messages.ValueKind != JsonValueKind.Array)
                return 0;

            // contacts[] carries the sender's WhatsApp profile name.
            var profileNames = ReadProfileNames(value);
            int stored = 0;

            foreach (var element in messages.EnumerateArray())
            {
                var from = element.TryGetProperty("from", out var f) ? f.GetString() : null;
                if (string.IsNullOrWhiteSpace(from)) continue;

                profileNames.TryGetValue(from!, out var profileName);
                var conversation = await _repository.GetOrCreateConversationAsync(from!, profileName);

                var kind = element.TryGetProperty("type", out var t)
                    ? t.GetString() ?? "text"
                    : "text";

                var message = new WaMessage
                {
                    ConversationId = conversation.VID,
                    WaId = from!,
                    MessageId = element.TryGetProperty("id", out var id) ? id.GetString() : null,
                    MessageKind = kind,
                    MessageTime = ParseUnixSeconds(
                        element.TryGetProperty("timestamp", out var ts) ? ts.GetString() : null),
                    ContextMessageId =
                        element.TryGetProperty("context", out var ctx) &&
                        ctx.TryGetProperty("id", out var ctxId)
                            ? ctxId.GetString()
                            : null
                };

                ReadContent(element, kind, message);

                if (await _repository.AddInboundAsync(message))
                {
                    stored++;
                    _logger.LogInformation(
                        "[WA] Inbound {Kind} from {From} ({Name})",
                        kind, from, conversation.ContactName ?? conversation.ProfileName ?? "unknown");
                }
            }

            return stored;
        }

        // Pulls the body/media fields for whichever message type arrived. Only
        // text can be composed today, but every type is recorded so an incoming
        // photo or voice note still shows up in the thread.
        private static void ReadContent(JsonElement element, string kind, WaMessage message)
        {
            switch (kind)
            {
                case "text":
                    if (element.TryGetProperty("text", out var text) &&
                        text.TryGetProperty("body", out var body))
                        message.Body = body.GetString();
                    break;

                case "button":
                    if (element.TryGetProperty("button", out var button) &&
                        button.TryGetProperty("text", out var buttonText))
                        message.Body = buttonText.GetString();
                    break;

                case "interactive":
                    // Reply from a list/button template.
                    if (element.TryGetProperty("interactive", out var interactive))
                    {
                        if (interactive.TryGetProperty("button_reply", out var br) &&
                            br.TryGetProperty("title", out var brTitle))
                            message.Body = brTitle.GetString();
                        else if (interactive.TryGetProperty("list_reply", out var lr) &&
                                 lr.TryGetProperty("title", out var lrTitle))
                            message.Body = lrTitle.GetString();
                    }
                    break;

                case "location":
                    if (element.TryGetProperty("location", out var location))
                    {
                        var lat = location.TryGetProperty("latitude", out var la) ? la.ToString() : "?";
                        var lng = location.TryGetProperty("longitude", out var lo) ? lo.ToString() : "?";
                        message.Body = $"{lat}, {lng}";
                    }
                    break;

                default:
                    // image / document / audio / video / sticker all share the
                    // same shape: an id, a mime_type and an optional caption.
                    if (element.TryGetProperty(kind, out var media))
                    {
                        if (media.TryGetProperty("id", out var mediaId))
                            message.MediaId = mediaId.GetString();
                        if (media.TryGetProperty("mime_type", out var mime))
                            message.MediaMimeType = mime.GetString();
                        if (media.TryGetProperty("filename", out var fileName))
                            message.MediaFileName = fileName.GetString();
                        if (media.TryGetProperty("caption", out var caption))
                            message.Body = caption.GetString();
                    }
                    break;
            }
        }

        private async Task<int> IngestStatusesAsync(JsonElement value)
        {
            if (!value.TryGetProperty("statuses", out var statuses) ||
                statuses.ValueKind != JsonValueKind.Array)
                return 0;

            int updated = 0;

            foreach (var status in statuses.EnumerateArray())
            {
                var messageId = status.TryGetProperty("id", out var id) ? id.GetString() : null;
                var state = status.TryGetProperty("status", out var s) ? s.GetString() : null;
                if (string.IsNullOrWhiteSpace(messageId) || string.IsNullOrWhiteSpace(state))
                    continue;

                var timestamp = ParseUnixSeconds(
                    status.TryGetProperty("timestamp", out var t) ? t.GetString() : null);

                string? errorCode = null, errorMessage = null;
                if (status.TryGetProperty("errors", out var errors) &&
                    errors.ValueKind == JsonValueKind.Array &&
                    errors.GetArrayLength() > 0)
                {
                    var first = errors[0];
                    if (first.TryGetProperty("code", out var c))
                        errorCode = c.ValueKind == JsonValueKind.Number
                            ? c.GetInt64().ToString()
                            : c.GetString();
                    if (first.TryGetProperty("title", out var title))
                        errorMessage = title.GetString();
                }

                if (await _repository.ApplyStatusAsync(
                        messageId!, state!, timestamp, errorCode, errorMessage))
                    updated++;
            }

            return updated;
        }

        private static Dictionary<string, string?> ReadProfileNames(JsonElement value)
        {
            var names = new Dictionary<string, string?>();

            if (!value.TryGetProperty("contacts", out var contacts) ||
                contacts.ValueKind != JsonValueKind.Array)
                return names;

            foreach (var contact in contacts.EnumerateArray())
            {
                var waId = contact.TryGetProperty("wa_id", out var w) ? w.GetString() : null;
                if (string.IsNullOrWhiteSpace(waId)) continue;

                names[waId!] = contact.TryGetProperty("profile", out var profile) &&
                               profile.TryGetProperty("name", out var name)
                    ? name.GetString()
                    : null;
            }

            return names;
        }

        // ─────────────────────────────────────────────────────────────
        // Chat list / thread
        // ─────────────────────────────────────────────────────────────
        public async Task<List<WaConversationDto>> GetConversationsAsync(string? search, int take)
        {
            var conversations = await _repository.GetConversationsAsync(search, take);
            return conversations.Select(ToDto).ToList();
        }

        public async Task<WaThreadDto?> GetThreadAsync(string waId, int? beforeVid, int take)
        {
            var normalized = ToWaId(waId);
            var conversation = await _repository.GetConversationAsync(normalized);
            if (conversation is null) return null;

            if (take < 1 || take > 200) take = 50;

            // Fetch one extra row to tell whether older messages remain.
            var page = await _repository.GetMessagesAsync(conversation.VID, beforeVid, take + 1);

            bool hasMore = page.Count > take;
            if (hasMore) page.RemoveAt(0);

            return new WaThreadDto
            {
                Conversation = ToDto(conversation),
                Messages = page.Select(ToDto).ToList(),
                HasMore = hasMore
            };
        }

        // ─────────────────────────────────────────────────────────────
        // Reply
        // ─────────────────────────────────────────────────────────────
        public async Task<(bool ok, string? error, WaMessageDto? message)> SendReplyAsync(
            string waId, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return (false, "Message is empty", null);

            var normalized = ToWaId(waId);
            var conversation = await _repository.GetConversationAsync(normalized);
            if (conversation is null)
                return (false, "No conversation with this contact yet", null);

            if (!IsWindowOpen(conversation.LastInboundAt))
            {
                return (false,
                    "The 24-hour reply window has closed. WhatsApp only allows an " +
                    "approved template message until this contact writes again.",
                    null);
            }

            var outcome = await _sender.SendTemplateAsync(normalized, new SmsTemplateRequest
            {
                // No template name — the sender posts this as free-form text,
                // which is exactly what the service window permits.
                Name = string.Empty,
                RenderedText = text
            });

            var message = new WaMessage
            {
                ConversationId = conversation.VID,
                WaId = normalized,
                MessageId = outcome.ProviderMessageId,
                MessageKind = "text",
                Body = text,
                Status = outcome.Success ? "accepted" : "failed",
                ErrorCode = outcome.ErrorCode,
                ErrorMessage = outcome.Error,
                MessageTime = DateTime.UtcNow
            };

            await _repository.AddOutboundAsync(message);

            return outcome.Success
                ? (true, null, ToDto(message))
                : (false, outcome.Error ?? "Send failed", ToDto(message));
        }

        public async Task<int> MarkReadAsync(string waId)
        {
            var conversation = await _repository.GetConversationAsync(ToWaId(waId));
            if (conversation is null) return 0;
            return await _repository.MarkReadAsync(conversation.VID);
        }

        public Task<int> GetTotalUnreadAsync() => _repository.GetTotalUnreadAsync();

        // ─────────────────────────────────────────────────────────────
        // Campaign mirror
        // ─────────────────────────────────────────────────────────────
        public async Task MirrorOutboundAsync(
            string phoneNumber, string? body, string? messageId,
            string? templateName, string? status, int? smsMessageVid)
        {
            try
            {
                var waId = ToWaId(phoneNumber);
                var conversation = await _repository.GetOrCreateConversationAsync(waId, null);

                await _repository.AddOutboundAsync(new WaMessage
                {
                    ConversationId = conversation.VID,
                    WaId = waId,
                    MessageId = messageId,
                    MessageKind = string.IsNullOrWhiteSpace(templateName) ? "text" : "template",
                    Body = body,
                    TemplateName = templateName,
                    Status = status,
                    SmsMessageVID = smsMessageVid,
                    MessageTime = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                // Mirroring is for display only — never fail a real send over it.
                _logger.LogWarning(ex, "[WA] Could not mirror outbound message to {Phone}", phoneNumber);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────

        // "0300-1234567" -> "923001234567" (country code, no leading 0, no '+').
        private string ToWaId(string phoneNumber)
        {
            var cleaned = new string(phoneNumber.Where(char.IsDigit).ToArray());
            if (cleaned.StartsWith("0"))
                cleaned = cleaned.Substring(1);
            if (!cleaned.StartsWith(_countryCode))
                cleaned = _countryCode + cleaned;
            return cleaned;
        }

        private static bool IsWindowOpen(DateTime? lastInboundAt)
            => lastInboundAt.HasValue &&
               DateTime.UtcNow - lastInboundAt.Value < ServiceWindow;

        private static DateTime ParseUnixSeconds(string? value)
            => long.TryParse(value, out var seconds)
                ? DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime
                : DateTime.UtcNow;

        private static WaConversationDto ToDto(WaConversation c)
        {
            bool open = IsWindowOpen(c.LastInboundAt);

            return new WaConversationDto
            {
                VID = c.VID,
                WaId = c.WaId,
                ContactName = c.ContactName,
                ProfileName = c.ProfileName,
                DisplayName = !string.IsNullOrWhiteSpace(c.ContactName) ? c.ContactName!
                            : !string.IsNullOrWhiteSpace(c.ProfileName) ? c.ProfileName!
                            : c.WaId,
                LastMessageAt = c.LastMessageAt,
                LastMessagePreview = c.LastMessagePreview,
                LastMessageDirection = c.LastMessageDirection?.ToString(),
                UnreadCount = c.UnreadCount,
                LastInboundAt = c.LastInboundAt,
                WindowOpen = open,
                WindowMinutesLeft = open
                    ? (int)(ServiceWindow - (DateTime.UtcNow - c.LastInboundAt!.Value)).TotalMinutes
                    : 0
            };
        }

        private static WaMessageDto ToDto(WaMessage m) => new()
        {
            VID = m.VID,
            Direction = m.Direction.ToString(),
            MessageId = m.MessageId,
            MessageKind = m.MessageKind,
            Body = m.Body,
            MediaId = m.MediaId,
            MediaMimeType = m.MediaMimeType,
            MediaFileName = m.MediaFileName,
            Status = m.Status,
            ErrorCode = m.ErrorCode,
            ErrorMessage = m.ErrorMessage,
            MessageTime = m.MessageTime,
            DeliveredAt = m.DeliveredAt,
            ReadAt = m.ReadAt,
            TemplateName = m.TemplateName
        };
    }
}

using School.API.Data.DBModels.SMS;

namespace School.API.Services.Interfaces
{
    // What a provider reported back for a single send. The queue processor
    // persists these fields on the SmsMessages row so a failed send can be
    // diagnosed from the messages viewer without digging through logs.
    public class SmsSendOutcome
    {
        public bool Success { get; set; }

        // Provider's own id for the message (Meta returns "wamid.xxx").
        public string? ProviderMessageId { get; set; }

        // Provider error code, e.g. Meta's "132000" (wrong parameter count).
        public string? ErrorCode { get; set; }

        public string? Error { get; set; }

        // Raw response body, truncated by the caller before storing.
        public string? RawResponse { get; set; }

        public static SmsSendOutcome Ok(string? messageId = null, string? raw = null)
            => new() { Success = true, ProviderMessageId = messageId, RawResponse = raw };

        public static SmsSendOutcome Fail(string error, string? errorCode = null, string? raw = null)
            => new() { Success = false, Error = error, ErrorCode = errorCode, RawResponse = raw };
    }

    // A pre-approved WhatsApp template plus the ordered values for its
    // {{1}}, {{2}}, ... body placeholders. Optionally carries a document that
    // becomes the template's header (used for result-card PDFs).
    public class SmsTemplateRequest
    {
        // Template name exactly as approved in the Meta Business account.
        public string Name { get; set; } = string.Empty;

        // Language code exactly as approved, e.g. "en", "en_US", "ur".
        public string Language { get; set; } = "en";

        // The queued row's message type (Absent, Fee, Result, ...). Lets a
        // routing sender (e.g. TypeRoutedSmsSender) pick a provider per type
        // without the sender needing to know about SmsMessage itself. Null
        // for callers outside the queue (e.g. a manual chat reply).
        public SmsMessageType? MessageType { get; set; }

        // Body parameters in placeholder order.
        public IReadOnlyList<string> BodyParams { get; set; } = Array.Empty<string>();

        // Optional document header (e.g. the result card PDF).
        public byte[]? DocumentBytes { get; set; }
        public string? DocumentFileName { get; set; }
        public string? DocumentMimeType { get; set; }

        public bool HasDocument => DocumentBytes is { Length: > 0 };

        // The template body with parameters already substituted. Meta ignores
        // this (it renders the approved body itself), but it is what we store
        // as FullMessage and what the non-template providers actually send.
        public string? RenderedText { get; set; }
    }

    public interface ISmsSender
    {
        // Recorded on each row so the viewer shows which gateway delivered it.
        // Derived from the class name unless a sender overrides it.
        string ProviderName
            => GetType().Name.Replace("SmsSender", "").Replace("Sender", "");

        Task<bool> SendAsync(string phoneNumber, string message);

        // Sends a document (e.g. a PDF result card) to a WhatsApp number with an
        // optional text caption. fileBytes is the raw file content.
        Task<bool> SendFileAsync(
            string phoneNumber,
            byte[] fileBytes,
            string fileName,
            string mimeType,
            string? caption);

        // Sends an approved template. Only Meta's Cloud API needs templates;
        // the unofficial gateways (WAHA / Wasender / Arsal) can post free text,
        // so the default implementation below simply sends the rendered body.
        // That keeps every existing provider working unchanged.
        async Task<SmsSendOutcome> SendTemplateAsync(
            string phoneNumber, SmsTemplateRequest request)
        {
            var text = request.RenderedText ?? string.Empty;

            bool ok = request.HasDocument
                ? await SendFileAsync(
                    phoneNumber,
                    request.DocumentBytes!,
                    request.DocumentFileName ?? "document.pdf",
                    request.DocumentMimeType ?? "application/pdf",
                    text)
                : await SendAsync(phoneNumber, text);

            return ok
                ? SmsSendOutcome.Ok()
                : SmsSendOutcome.Fail("Sender returned false");
        }
    }
}

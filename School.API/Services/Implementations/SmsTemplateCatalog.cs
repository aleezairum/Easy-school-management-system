using School.API.Data.DBModels.SMS;
using School.API.Services.Interfaces;

namespace School.API.Services.Implementations
{
    // Reads MetaWhatsApp:Templates once at startup and hands out the approved
    // template for each message type. A type with a blank/missing Name simply
    // has no template configured and falls back to free-form text.
    public class SmsTemplateCatalog : ISmsTemplateCatalog
    {
        private readonly Dictionary<SmsMessageType, SmsTemplateDefinition> _templates = new();
        private readonly ILogger<SmsTemplateCatalog> _logger;

        public bool IsEnabled => _templates.Count > 0;

        public SmsTemplateCatalog(IConfiguration configuration, ILogger<SmsTemplateCatalog> logger)
        {
            _logger = logger;

            var defaultLanguage = configuration["MetaWhatsApp:DefaultLanguage"] ?? "en";
            var section = configuration.GetSection("MetaWhatsApp:Templates");

            foreach (var child in section.GetChildren())
            {
                if (!Enum.TryParse<SmsMessageType>(child.Key, ignoreCase: true, out var messageType))
                {
                    _logger.LogWarning(
                        "[Meta] Ignoring template entry '{Key}' — not a known SmsMessageType", child.Key);
                    continue;
                }

                var name = child["Name"];
                if (string.IsNullOrWhiteSpace(name))
                    continue; // configured but intentionally left blank

                _templates[messageType] = new SmsTemplateDefinition
                {
                    Name = name.Trim(),
                    Language = (child["Language"] ?? defaultLanguage).Trim()
                };
            }

            if (_templates.Count == 0)
            {
                _logger.LogInformation(
                    "[Meta] No WhatsApp templates configured — messages will be sent as free-form text.");
            }
            else
            {
                _logger.LogInformation(
                    "[Meta] Loaded {Count} WhatsApp template(s): {Templates}",
                    _templates.Count,
                    string.Join(", ", _templates.Select(t => $"{t.Key}={t.Value.Name}/{t.Value.Language}")));
            }
        }

        public SmsTemplateDefinition? Resolve(SmsMessageType messageType)
            => _templates.TryGetValue(messageType, out var definition) ? definition : null;
    }
}

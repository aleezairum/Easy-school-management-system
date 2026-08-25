using School.API.Data.DBModels.SMS;

namespace School.API.Services.Interfaces
{
    // Maps an SmsMessageType onto the approved Meta template registered for it.
    // Names/languages live in appsettings.json under MetaWhatsApp:Templates so a
    // template can be renamed or re-approved without a code change.
    public interface ISmsTemplateCatalog
    {
        // True once templates are configured — i.e. the school is on Meta's
        // official Cloud API. When false the pipeline keeps sending free-form
        // text through whichever legacy gateway is registered.
        bool IsEnabled { get; }

        // Returns the approved template for this message type, or null when
        // none is configured (that type keeps going out as free-form text).
        SmsTemplateDefinition? Resolve(SmsMessageType messageType);
    }

    public class SmsTemplateDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Language { get; set; } = "en";
    }
}

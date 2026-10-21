using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace School.API.Serialization
{
    /// <summary>
    /// Central policy for how <see cref="DateTime"/> values cross the API boundary.
    /// </summary>
    /// <remarks>
    /// The database stores instants in UTC (GETUTCDATE / DateTime.UtcNow), but EF
    /// hands them back with <see cref="DateTimeKind.Unspecified"/>. Serialized as-is
    /// they reach the browser without a "Z", so <c>new Date(...)</c> reads them as
    /// local time and every timestamp renders <em>offset hours behind</em> — five in
    /// Pakistan (UTC+5). Stamping the "Z" here lets the browser convert to whatever
    /// timezone the viewer's machine is set to, with no per-view code.
    ///
    /// Two kinds of DateTime live in this schema and they must NOT be treated alike:
    ///
    ///   instant  — CreatedAt, InsertedDate, LastRunAt, SentAt ... a moment in time.
    ///              Genuinely UTC, and must be converted for display.
    ///
    ///   calendar — DateOfBirth, JoiningDate, DueDate, AttDate, VDate ... a date on
    ///              a calendar. A birthday has no timezone. Converting one shifts it
    ///              a day backwards for any viewer west of UTC, so these are written
    ///              as a bare "yyyy-MM-dd" that carries no zone to convert.
    ///
    ///   local    — EventTime, LastEventTime: a time read off a clock at the school
    ///              (an attendance machine's punch). Kept as the wall-clock time it
    ///              was, written "yyyy-MM-ddTHH:mm:ss" with no zone, so the browser
    ///              shows it exactly as the machine recorded it.
    ///
    /// Applied through the type-info resolver rather than [JsonConverter] attributes
    /// so it covers every DTO and entity — including ones added later — from a single
    /// place, with nothing to remember to annotate.
    /// </remarks>
    public static class DateTimeJsonPolicy
    {
        private static readonly UtcInstantConverter Instant = new();
        private static readonly NullableUtcInstantConverter NullableInstant = new();
        private static readonly CalendarDateConverter Calendar = new();
        private static readonly NullableCalendarDateConverter NullableCalendar = new();
        private static readonly LocalTimeConverter Local = new();
        private static readonly NullableLocalTimeConverter NullableLocal = new();

        private const string InstantFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";
        private const string CalendarFormat = "yyyy-MM-dd";
        private const string LocalFormat = "yyyy-MM-dd'T'HH:mm:ss";

        /// <summary>Wall-clock times from the school's own clocks (see "local" above).</summary>
        private static readonly HashSet<string> LocalNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "EventTime",
            "LastEventTime"
        };

        /// <summary>
        /// Instants whose name does not end in "At". Anything not listed here and
        /// not ending in "At" is treated as a calendar date.
        /// </summary>
        private static readonly HashSet<string> InstantNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "InsertedDate",
            "UpdatedDate",
            "CreatedDate",
            "MessageTime"
        };

        public static void Apply(JsonTypeInfo typeInfo)
        {
            foreach (var prop in typeInfo.Properties)
            {
                if (prop.PropertyType == typeof(DateTime))
                    prop.CustomConverter = LocalNames.Contains(prop.Name) ? Local : IsInstant(prop.Name) ? Instant : Calendar;
                else if (prop.PropertyType == typeof(DateTime?))
                    prop.CustomConverter = LocalNames.Contains(prop.Name) ? NullableLocal : IsInstant(prop.Name) ? NullableInstant : NullableCalendar;
            }
        }

        // The "At" suffix is the convention every instant in this schema follows
        // (CreatedAt, SentAt, LockedAt, LastRunAt ...), so new ones are handled
        // correctly without touching this file. No calendar-date name ends in "At".
        private static bool IsInstant(string name) =>
            name.EndsWith("At", StringComparison.OrdinalIgnoreCase) || InstantNames.Contains(name);

        internal static string WriteInstant(DateTime value)
        {
            var utc = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                // EF materialises as Unspecified; the column is already UTC.
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };

            return utc.ToString(InstantFormat, CultureInfo.InvariantCulture);
        }

        internal static string WriteCalendarDate(DateTime value) =>
            value.ToString(CalendarFormat, CultureInfo.InvariantCulture);

        internal static string WriteLocal(DateTime value) =>
            value.ToString(LocalFormat, CultureInfo.InvariantCulture);

        internal static DateTime ReadLocal(string? text) =>
            !string.IsNullOrWhiteSpace(text) && DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var v)
                ? DateTime.SpecifyKind(v, DateTimeKind.Unspecified)
                : default;

        internal static DateTime ReadInstant(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return default;

            // AssumeUniversal covers a payload with no offset (our own format);
            // AdjustToUniversal normalises one that does carry an offset.
            return DateTime.TryParse(text, CultureInfo.InvariantCulture,
                       DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed)
                ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
                : default;
        }

        internal static DateTime ReadCalendarDate(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return default;

            // Read the calendar date exactly as written and ignore any time or
            // offset that follows it. Deliberately NOT a zone-aware parse: doing
            // that to "1990-05-15T00:00:00Z" west of UTC would yield the 14th.
            if (text.Length >= 10 && DateTime.TryParseExact(text.Substring(0, 10), CalendarFormat,
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
                return exact;

            return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var loose)
                ? loose.Date
                : default;
        }
    }

    /// <summary>Writes a UTC instant as ISO-8601 with an explicit "Z".</summary>
    internal sealed class UtcInstantConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions o) =>
            DateTimeJsonPolicy.ReadInstant(reader.GetString());

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions o) =>
            writer.WriteStringValue(DateTimeJsonPolicy.WriteInstant(value));
    }

    internal sealed class NullableUtcInstantConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions o)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;

            var text = reader.GetString();
            return string.IsNullOrWhiteSpace(text) ? null : DateTimeJsonPolicy.ReadInstant(text);
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions o)
        {
            if (value is null) writer.WriteNullValue();
            else writer.WriteStringValue(DateTimeJsonPolicy.WriteInstant(value.Value));
        }
    }

    /// <summary>Writes a calendar date as a bare "yyyy-MM-dd" — no time, no zone.</summary>
    internal sealed class CalendarDateConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions o) =>
            DateTimeJsonPolicy.ReadCalendarDate(reader.GetString());

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions o) =>
            writer.WriteStringValue(DateTimeJsonPolicy.WriteCalendarDate(value));
    }

    internal sealed class NullableCalendarDateConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions o)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;

            var text = reader.GetString();
            return string.IsNullOrWhiteSpace(text) ? null : DateTimeJsonPolicy.ReadCalendarDate(text);
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions o)
        {
            if (value is null) writer.WriteNullValue();
            else writer.WriteStringValue(DateTimeJsonPolicy.WriteCalendarDate(value.Value));
        }
    }

    /// <summary>Writes a school wall-clock time as "yyyy-MM-ddTHH:mm:ss" - no zone.</summary>
    internal sealed class LocalTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions o) =>
            DateTimeJsonPolicy.ReadLocal(reader.GetString());

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions o) =>
            writer.WriteStringValue(DateTimeJsonPolicy.WriteLocal(value));
    }

    internal sealed class NullableLocalTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions o)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;

            var text = reader.GetString();
            return string.IsNullOrWhiteSpace(text) ? null : DateTimeJsonPolicy.ReadLocal(text);
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions o)
        {
            if (value is null) writer.WriteNullValue();
            else writer.WriteStringValue(DateTimeJsonPolicy.WriteLocal(value.Value));
        }
    }
}

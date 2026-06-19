using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HvacController.Services;

public sealed class EasternDateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
{
    private const string OutputFormat = "yyyy-MM-dd'T'HH:mm:ss";

    private static readonly TimeZoneInfo EasternTimeZone = GetEasternTimeZone();

    public override DateTimeOffset Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var value = reader.GetString();

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException("Expected a date/time value.");
        }

        var parsed = DateTime.Parse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal);

        return new DateTimeOffset(parsed);
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateTimeOffset value,
        JsonSerializerOptions options)
    {
        var eastern = TimeZoneInfo.ConvertTime(value, EasternTimeZone);

        writer.WriteStringValue(
            eastern.ToString(OutputFormat, CultureInfo.InvariantCulture));
    }

    private static TimeZoneInfo GetEasternTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        }
    }
}

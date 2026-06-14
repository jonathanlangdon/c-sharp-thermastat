using System.Text.Json;
using HvacController.Models;

namespace HvacController.Services;

public static class UpstairsSensorMessageParser
{
    public static UpstairsSensorMessage? Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!TryGetRequiredDouble(root, "temperatureFahr", out var temperatureFahr))
            {
                return null;
            }

            if (!TryGetRequiredDouble(root, "relativeHumidity", out var relativeHumidity))
            {
                return null;
            }

            var motionDetected = TryGetOptionalBool(
                root,
                "motionDetected");

            var mode = TryGetOptionalMode(root, "mode");

            if (mode.Invalid)
            {
                return null;
            }

            return new UpstairsSensorMessage
            {
                TemperatureFahr = temperatureFahr,
                RelativeHumidity = relativeHumidity,
                MotionDetected = motionDetected,
                Mode = mode.Value
            };
        }
        catch (JsonException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static bool TryGetRequiredDouble(
        JsonElement root,
        string propertyName,
        out double value)
    {
        value = 0;

        if (!root.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        if (property.ValueKind != JsonValueKind.Number)
        {
            return false;
        }

        value = property.GetDouble();
        return true;
    }

    private static bool? TryGetOptionalBool(
        JsonElement root,
        string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (property.ValueKind != JsonValueKind.True &&
            property.ValueKind != JsonValueKind.False)
        {
            return null;
        }

        return property.GetBoolean();
    }

    private static ModeParseResult TryGetOptionalMode(
        JsonElement root,
        string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
        {
            return new ModeParseResult(null, false);
        }

        if (property.ValueKind == JsonValueKind.Null)
        {
            return new ModeParseResult(null, false);
        }

        if (property.ValueKind != JsonValueKind.String)
        {
            return new ModeParseResult(null, true);
        }

        var value = property.GetString();

        if (!Enum.TryParse<HvacMode>(
                value,
                ignoreCase: true,
                out var mode))
        {
            return new ModeParseResult(null, true);
        }

        return new ModeParseResult(mode, false);
    }

    private sealed record ModeParseResult(
        HvacMode? Value,
        bool Invalid);
}

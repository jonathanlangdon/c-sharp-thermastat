using System.Text.Json;
using HvacController.Models;

namespace HvacController.Services;

public sealed class SettingsSetMessageHandler
{
    private readonly IThermostatPersistentStateStore _stateStore;

    public SettingsSetMessageHandler(
        IThermostatPersistentStateStore stateStore)
    {
        _stateStore = stateStore;
    }

    public bool Handle(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            var current = _stateStore.Load();

            if (!TryGetDouble(root, "heatSetPointDay", out var heatSetPointDay) ||
                !TryGetDouble(root, "heatSetPointNight", out var heatSetPointNight) ||
                !TryGetDouble(root, "humidityTargetFair", out var humidityTargetFair) ||
                !TryGetDouble(root, "humidityTargetGood", out var humidityTargetGood) ||
                !TryGetDouble(root, "humidityTargetIdeal", out var humidityTargetIdeal) ||
                !TryGetBool(root, "manualOverride", out var manualOverride) ||
                !TryGetManualMode(root, "manualMode", out var manualMode))
            {
                return false;
            }

            if (!IsValidHeatSetPoint(heatSetPointDay) ||
                !IsValidHeatSetPoint(heatSetPointNight) ||
                !IsValidHumidityTarget(humidityTargetFair) ||
                !IsValidHumidityTarget(humidityTargetGood) ||
                !IsValidHumidityTarget(humidityTargetIdeal))
            {
                return false;
            }

            var updated = current with
            {
                HeatSetPointDay = heatSetPointDay,
                HeatSetPointNight = heatSetPointNight,

                HumidityTargetFair = humidityTargetFair,
                HumidityTargetGood = humidityTargetGood,
                HumidityTargetIdeal = humidityTargetIdeal,

                ManualOverride = manualOverride,
                ManualMode = manualMode
            };

            _stateStore.Save(updated);

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryGetDouble(
        JsonElement root,
        string propertyName,
        out double value)
    {
        value = 0;

        return root.TryGetProperty(propertyName, out var property) &&
               property.ValueKind == JsonValueKind.Number &&
               property.TryGetDouble(out value);
    }

    private static bool TryGetBool(
        JsonElement root,
        string propertyName,
        out bool value)
    {
        value = false;

        if (!root.TryGetProperty(propertyName, out var property) ||
            property.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
        {
            return false;
        }

        value = property.GetBoolean();
        return true;
    }

    private static bool TryGetManualMode(
        JsonElement root,
        string propertyName,
        out ManualMode value)
    {
        value = ManualMode.Off;

        if (!root.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var rawValue = property.GetString()?.Trim();

        return Enum.TryParse(
            rawValue,
            ignoreCase: true,
            out value);
    }

    private static bool IsValidHeatSetPoint(double value)
    {
        return value >= 50.0 && value <= 85.0;
    }

    private static bool IsValidHumidityTarget(double value)
    {
        return value >= 0.0 && value <= 30.0;
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;
using HvacController.Models;

namespace HvacController.Services;

public sealed class ThermostatPersistentStateStore : IThermostatPersistentStateStore
{
    private const string StateFilePath = "state/thermostat-state.json";

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
            new EasternDateTimeOffsetJsonConverter()
        }
    };

    public ThermostatPersistentState Load()
    {
        if (!File.Exists(StateFilePath))
        {
            return new ThermostatPersistentState();
        }

        try
        {
            var json = File.ReadAllText(StateFilePath);

            return JsonSerializer.Deserialize<ThermostatPersistentState>(
                       json,
                       Options) ??
                   new ThermostatPersistentState();
        }
        catch
        {
            return new ThermostatPersistentState();
        }
    }

    public void Save(ThermostatPersistentState state)
    {
        var directory = Path.GetDirectoryName(StateFilePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(state, Options);

        File.WriteAllText(StateFilePath, json);
    }
}

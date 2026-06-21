using System.Text.Json;
using HvacController.Models;

namespace HvacController.Services;

public sealed class ModeSetMessageHandler
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IThermostatPersistentStateStore _persistentStateStore;

    public ModeSetMessageHandler(
        IThermostatPersistentStateStore persistentStateStore)
    {
        _persistentStateStore = persistentStateStore;
    }

    public bool Handle(string payload)
    {
        try
        {
            var message = JsonSerializer.Deserialize<ModeSetMessage>(
                payload,
                Options);

            if (message?.Mode is null)
            {
                return false;
            }

            var mode = message.Mode.Trim();

            HvacMode parsedMode;

            if (mode.Equals("Heat", StringComparison.OrdinalIgnoreCase))
            {
                parsedMode = HvacMode.Heat;
            }
            else if (mode.Equals("Cool", StringComparison.OrdinalIgnoreCase))
            {
                parsedMode = HvacMode.Cool;
            }
            else
            {
                return false;
            }

            var current = _persistentStateStore.Load();

            var updated = current with
            {
                Mode = parsedMode
            };

            _persistentStateStore.Save(updated);

            Console.WriteLine($"Mode set by MQTT command: {parsedMode}");

            return true;
        }
        catch
        {
            return false;
        }
    }
}
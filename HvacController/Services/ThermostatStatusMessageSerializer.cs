using System.Text.Json;
using System.Text.Json.Serialization;
using HvacController.Models;

namespace HvacController.Services;

public static class ThermostatStatusMessageSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public static string Serialize(ThermostatStatusMessage message)
    {
        return JsonSerializer.Serialize(message, Options);
    }
}

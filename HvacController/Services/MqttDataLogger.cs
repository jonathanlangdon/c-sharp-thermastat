using System.Text.Json;
using System.Text.Json.Serialization;
using HvacController.Models;

namespace HvacController.Services;

public sealed class MqttDataLogger
{
    public const string UpstairsSensorTopic = "hvac/upstairs/sensor";
    public const string HvacStatusTopic = "hvac/status";

    private readonly ICsvLogWriter _logWriter;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public MqttDataLogger(ICsvLogWriter logWriter)
    {
        _logWriter = logWriter;
    }

    public async Task HandleMessageAsync(
        string topic,
        string payload,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        if (topic == UpstairsSensorTopic)
        {
            var message = JsonSerializer.Deserialize<UpstairsSensorMessage>(
                payload,
                JsonOptions);

            if (message is null)
                return;

            await _logWriter.AppendLineAsync(
                "upstairs-sensor.csv",
                UpstairsSensorCsvFormatter.Header,
                UpstairsSensorCsvFormatter.Format(timestamp, message),
                cancellationToken);

            return;
        }

        if (topic == HvacStatusTopic)
        {
            var message = JsonSerializer.Deserialize<ThermostatStatusMessage>(
                payload,
                JsonOptions);

            if (message is null)
                return;

            await _logWriter.AppendLineAsync(
                "hvac-status.csv",
                HvacStatusCsvFormatter.Header,
                HvacStatusCsvFormatter.Format(timestamp, message),
                cancellationToken);
        }
    }
}
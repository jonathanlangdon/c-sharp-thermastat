using HvacController.Models;

namespace HvacController.Services;

public sealed class MqttThermostatStatusPublisher : IThermostatStatusPublisher
{
    private const string Topic = "hvac/status";

    private readonly IMqttMessagePublisher _mqttPublisher;

    public MqttThermostatStatusPublisher(IMqttMessagePublisher mqttPublisher)
    {
        _mqttPublisher = mqttPublisher;
    }

    public Task PublishAsync(
        ThermostatStatusMessage message,
        CancellationToken cancellationToken)
    {
        var payload = ThermostatStatusMessageSerializer.Serialize(message);

        return _mqttPublisher.PublishAsync(
            Topic,
            payload,
            cancellationToken);
    }
}

using System.Text.Json;
using HvacController.Models;
using HvacController.Services;

namespace HvacController.Tests;

public sealed class MqttThermostatStatusPublisherTests
{
    [Fact]
    public async Task PublishAsync_PublishesStatusToHvacStatusTopic()
    {
        var mqttPublisher = new FakeMqttMessagePublisher();
        var publisher = new MqttThermostatStatusPublisher(mqttPublisher);

        var message = new ThermostatStatusMessage
        {
            Heat = false,
            Cool = true,
            Fan = true,
            Reason = "Everything Normal",
            Mode = HvacMode.Cool,
            UpstairsTemperature = 72.1,
            UpstairsRelativeHumidity = 50.0,
            UpstairsAbsoluteHumidity = 9.83,
            DownstairsTemperature = 66.4,
            DownstairsRelativeHumidity = 55.2,
            DownstairsAbsoluteHumidity = 8.16,
            ControlAbsoluteHumidity = 9.83,
            OutsideTemperature = 70.0,
            OutsideAbsoluteHumidity = 9.4
        };

        await publisher.PublishAsync(message, CancellationToken.None);

        Assert.Equal("hvac/status", mqttPublisher.Topic);

        using var document = JsonDocument.Parse(mqttPublisher.Payload!);
        var root = document.RootElement;

        Assert.True(root.GetProperty("cool").GetBoolean());
        Assert.True(root.GetProperty("fan").GetBoolean());
        Assert.True(mqttPublisher.Retain);
        Assert.Equal("hvac/status", mqttPublisher.Topic);
        Assert.Equal("Cool", root.GetProperty("mode").GetString());
        Assert.Equal(9.83, root.GetProperty("controlAbsoluteHumidity").GetDouble());
        Assert.Equal(8.16, root.GetProperty("downstairsAbsoluteHumidity").GetDouble());
    }

    private sealed class FakeMqttMessagePublisher : IMqttMessagePublisher
    {
        public string? Topic { get; private set; }
        public string? Payload { get; private set; }
        public bool Retain { get; private set; }

        public Task PublishAsync(
            string topic,
            string payload,
            bool retain,
            CancellationToken cancellationToken)
        {
            Topic = topic;
            Payload = payload;
            Retain = retain;

            return Task.CompletedTask;
        }
    }
}

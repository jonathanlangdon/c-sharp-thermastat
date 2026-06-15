using System.Text;
using MQTTnet;

namespace HvacController.Services;

public sealed class MqttMessagePublisher : IMqttMessagePublisher
{
    private readonly string _host;
    private readonly int _port;

    public MqttMessagePublisher()
        : this("localhost", 1883)
    {
    }

    public MqttMessagePublisher(string host, int port)
    {
        _host = host;
        _port = port;
    }

    public async Task PublishAsync(
        string topic,
        string payload,
        bool retain,
        CancellationToken cancellationToken)
    {
        var mqttFactory = new MqttClientFactory();
        using var mqttClient = mqttFactory.CreateMqttClient();

        var options = mqttFactory
            .CreateClientOptionsBuilder()
            .WithClientId($"hvac-controller-status-{Guid.NewGuid():N}")
            .WithTcpServer(_host, _port)
            .Build();

        await mqttClient.ConnectAsync(options, cancellationToken);

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(Encoding.UTF8.GetBytes(payload))
            .WithRetainFlag(retain)
            .Build();

        await mqttClient.PublishAsync(message, cancellationToken);

        await mqttClient.DisconnectAsync();
    }
}

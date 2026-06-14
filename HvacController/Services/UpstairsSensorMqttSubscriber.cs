using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using System.Buffers;

namespace HvacController.Services;

public sealed class UpstairsSensorMqttSubscriber : BackgroundService
{
    private const string Topic = "hvac/upstairs/sensor";

    private readonly UpstairsSensorMessageHandler _handler;
    private readonly ILogger<UpstairsSensorMqttSubscriber> _logger;

    public UpstairsSensorMqttSubscriber(
        UpstairsSensorMessageHandler handler,
        ILogger<UpstairsSensorMqttSubscriber> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mqttFactory = new MqttClientFactory();
        using var mqttClient = mqttFactory.CreateMqttClient();

        mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload.ToArray());

            var handled = _handler.Handle(
                payload,
                DateTimeOffset.Now);

            if (handled)
            {
                _logger.LogInformation(
                    "Handled upstairs sensor MQTT message from topic {Topic}.",
                    e.ApplicationMessage.Topic);
            }
            else
            {
                _logger.LogWarning(
                    "Ignored invalid upstairs sensor MQTT message from topic {Topic}: {Payload}",
                    e.ApplicationMessage.Topic,
                    payload);
            }

            return Task.CompletedTask;
        };

        var options = mqttFactory
            .CreateClientOptionsBuilder()
            .WithClientId("hvac-controller")
            .WithTcpServer("localhost", 1883)
            .Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!mqttClient.IsConnected)
                {
                    _logger.LogInformation("Connecting to MQTT broker.");

                    await mqttClient.ConnectAsync(
                        options,
                        stoppingToken);

                    var subscribeOptions = mqttFactory
                        .CreateSubscribeOptionsBuilder()
                        .WithTopicFilter(Topic)
                        .Build();

                    await mqttClient.SubscribeAsync(
                        subscribeOptions,
                        stoppingToken);

                    _logger.LogInformation(
                        "Subscribed to MQTT topic {Topic}.",
                        Topic);
                }

                await Task.Delay(
                    TimeSpan.FromSeconds(5),
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown.
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "MQTT subscriber error. Retrying in 10 seconds.");

                await Task.Delay(
                    TimeSpan.FromSeconds(10),
                    stoppingToken);
            }
        }

        if (mqttClient.IsConnected)
        {
            await mqttClient.DisconnectAsync();
        }
    }
}
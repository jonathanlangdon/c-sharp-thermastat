using System.Text;
using MQTTnet;
using System.Buffers;

namespace HvacController.Services;

public sealed class UpstairsSensorMqttSubscriber : BackgroundService
{
    private const string UpstairsSensorTopic = "hvac/upstairs/sensor";
    private const string ModeSetTopic = "hvac/mode/set";

    private readonly UpstairsSensorMessageHandler _upstairsSensorHandler;
    private readonly ModeSetMessageHandler _modeSetHandler;
    private readonly ILogger<UpstairsSensorMqttSubscriber> _logger;

    public UpstairsSensorMqttSubscriber(
        UpstairsSensorMessageHandler upstairsSensorHandler,
        ModeSetMessageHandler modeSetHandler,
        ILogger<UpstairsSensorMqttSubscriber> logger)
    {
        _upstairsSensorHandler = upstairsSensorHandler;
        _modeSetHandler = modeSetHandler;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mqttFactory = new MqttClientFactory();
        using var mqttClient = mqttFactory.CreateMqttClient();

        mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(
                e.ApplicationMessage.Payload.ToArray());

            if (topic == UpstairsSensorTopic)
            {
                var handled = _upstairsSensorHandler.Handle(
                    payload,
                    DateTimeOffset.Now);

                if (handled)
                {
                    _logger.LogInformation(
                        "Handled upstairs sensor MQTT message from topic {Topic}.",
                        topic);
                }
                else
                {
                    _logger.LogWarning(
                        "Ignored invalid upstairs sensor MQTT message from topic {Topic}: {Payload}",
                        topic,
                        payload);
                }

                return Task.CompletedTask;
            }

            if (topic == ModeSetTopic)
            {
                var handled = _modeSetHandler.Handle(payload);

                if (handled)
                {
                    _logger.LogInformation(
                        "Handled mode set MQTT message from topic {Topic}.",
                        topic);
                }
                else
                {
                    _logger.LogWarning(
                        "Ignored invalid mode set MQTT message from topic {Topic}: {Payload}",
                        topic,
                        payload);
                }

                return Task.CompletedTask;
            }

            _logger.LogWarning(
                "Ignored MQTT message from unexpected topic {Topic}: {Payload}",
                topic,
                payload);

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
                        .WithTopicFilter(UpstairsSensorTopic)
                        .WithTopicFilter(ModeSetTopic)
                        .Build();

                    await mqttClient.SubscribeAsync(
                        subscribeOptions,
                        stoppingToken);

                    _logger.LogInformation(
                        "Subscribed to MQTT topics {UpstairsSensorTopic} and {ModeSetTopic}.",
                        UpstairsSensorTopic,
                        ModeSetTopic);
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
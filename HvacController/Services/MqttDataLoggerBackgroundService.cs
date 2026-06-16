using System.Buffers;
using System.Text;
using MQTTnet;

namespace HvacController.Services;

public sealed class MqttDataLoggerBackgroundService : BackgroundService
{
    private readonly ILogger<MqttDataLoggerBackgroundService> _logger;
    private readonly MqttDataLogger _dataLogger;

    public MqttDataLoggerBackgroundService(
        ILogger<MqttDataLoggerBackgroundService> logger,
        MqttDataLogger dataLogger)
    {
        _logger = logger;
        _dataLogger = dataLogger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mqttFactory = new MqttClientFactory();

        while (!stoppingToken.IsCancellationRequested)
        {
            using var mqttClient = mqttFactory.CreateMqttClient();

            mqttClient.ApplicationMessageReceivedAsync += async e =>
            {
                var topic = e.ApplicationMessage.Topic;

                var payload = Encoding.UTF8.GetString(
                    e.ApplicationMessage.Payload.ToArray());

                try
                {
                    await _dataLogger.HandleMessageAsync(
                        topic,
                        payload,
                        DateTimeOffset.Now,
                        stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to log MQTT message from topic {Topic}",
                        topic);
                }
            };

            try
            {
                var options = mqttFactory
                    .CreateClientOptionsBuilder()
                    .WithClientId("hvac-data-logger")
                    .WithTcpServer("localhost", 1883)
                    .Build();

                await mqttClient.ConnectAsync(options, stoppingToken);

                var subscribeOptions = mqttFactory
                    .CreateSubscribeOptionsBuilder()
                    .WithTopicFilter(MqttDataLogger.UpstairsSensorTopic)
                    .WithTopicFilter(MqttDataLogger.HvacStatusTopic)
                    .Build();

                await mqttClient.SubscribeAsync(
                    subscribeOptions,
                    stoppingToken);

                _logger.LogInformation(
                    "MQTT data logger subscribed to {UpstairsTopic} and {StatusTopic}",
                    MqttDataLogger.UpstairsSensorTopic,
                    MqttDataLogger.HvacStatusTopic);

                while (mqttClient.IsConnected &&
                       !stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "MQTT data logger disconnected. Retrying in 5 seconds.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}

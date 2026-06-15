namespace HvacController.Services;

public interface IMqttMessagePublisher
{
    Task PublishAsync(
        string topic,
        string payload,
        CancellationToken cancellationToken);
}

using HvacController.Models;

namespace HvacController.Services;

public interface IThermostatStatusPublisher
{
    Task PublishAsync(
        ThermostatStatusMessage message,
        CancellationToken cancellationToken);
}

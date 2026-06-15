using HvacController.Models;

namespace HvacController.Services;

public sealed class NoOpThermostatStatusPublisher : IThermostatStatusPublisher
{
    public Task PublishAsync(
        ThermostatStatusMessage message,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

using HvacController.Models;

namespace HvacController.Services;

public sealed class NoOpDownstairsSensorReader : IDownstairsSensorReader
{
    public Task<DownstairsSensorReading?> ReadAsync(
        CancellationToken cancellationToken)
    {
        return Task.FromResult<DownstairsSensorReading?>(null);
    }
}

using HvacController.Models;

namespace HvacController.Services;

public interface IDownstairsSensorReader
{
    Task<DownstairsSensorReading?> ReadAsync(
        CancellationToken cancellationToken);
}

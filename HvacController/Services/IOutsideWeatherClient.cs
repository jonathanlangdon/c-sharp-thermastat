using HvacController.Models;

namespace HvacController.Services;

public interface IOutsideWeatherClient
{
    Task<OutsideWeatherReading?> GetLatestAsync(
        CancellationToken cancellationToken);
}
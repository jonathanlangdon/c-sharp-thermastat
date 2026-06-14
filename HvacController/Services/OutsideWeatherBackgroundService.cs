using Microsoft.Extensions.Hosting;

namespace HvacController.Services;

public sealed class OutsideWeatherBackgroundService : BackgroundService
{
    private readonly IOutsideWeatherUpdater _updater;
    private readonly TimeSpan _refreshInterval;

    public OutsideWeatherBackgroundService(IOutsideWeatherUpdater updater)
        : this(updater, TimeSpan.FromMinutes(5))
    {
    }

    public OutsideWeatherBackgroundService(
        IOutsideWeatherUpdater updater,
        TimeSpan refreshInterval)
    {
        _updater = updater;
        _refreshInterval = refreshInterval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _updater.RefreshAsync(stoppingToken);

            try
            {
                await Task.Delay(_refreshInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Normal shutdown.
            }
        }
    }
}
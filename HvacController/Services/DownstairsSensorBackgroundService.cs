using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HvacController.Services;

public sealed class DownstairsSensorBackgroundService : BackgroundService
{
    private readonly IDownstairsSensorUpdater _updater;
    private readonly TimeSpan _refreshInterval;
    private readonly ILogger<DownstairsSensorBackgroundService>? _logger;

    public DownstairsSensorBackgroundService(
        IDownstairsSensorUpdater updater,
        ILogger<DownstairsSensorBackgroundService> logger)
        : this(updater, TimeSpan.FromSeconds(30), logger)
    {
    }

    public DownstairsSensorBackgroundService(
        IDownstairsSensorUpdater updater,
        TimeSpan refreshInterval)
        : this(updater, refreshInterval, null)
    {
    }

    private DownstairsSensorBackgroundService(
        IDownstairsSensorUpdater updater,
        TimeSpan refreshInterval,
        ILogger<DownstairsSensorBackgroundService>? logger)
    {
        _updater = updater;
        _refreshInterval = refreshInterval;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _updater.RefreshAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown.
            }
            catch (Exception exception)
            {
                _logger?.LogWarning(
                    exception,
                    "Downstairs sensor refresh failed.");
            }

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

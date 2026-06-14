using HvacController.Services;

namespace HvacController;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IRelayService _relays;

    public Worker(
        ILogger<Worker> logger,
        IRelayService relays)
    {
        _logger = logger;
        _relays = relays;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HVAC controller relay test starting.");

        _logger.LogInformation("All relays OFF.");
        _relays.AllOff();
        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

        _logger.LogInformation("Heat relay ON for 2 seconds.");
        _relays.SetRelays(heat: true, cool: false, fan: false);
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        _logger.LogInformation("All relays OFF.");
        _relays.AllOff();
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        _logger.LogInformation("Cool + fan relays ON for 2 seconds.");
        _relays.SetRelays(heat: false, cool: true, fan: true);
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        _logger.LogInformation("All relays OFF.");
        _relays.AllOff();
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        _logger.LogInformation("Fan relay ON for 2 seconds.");
        _relays.SetRelays(heat: false, cool: false, fan: true);
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        _logger.LogInformation("All relays OFF.");
        _relays.AllOff();

        _logger.LogInformation("Relay test complete. Idling with all relays OFF.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping HVAC controller. Turning all relays OFF.");
        _relays.AllOff();

        return base.StopAsync(cancellationToken);
    }
}
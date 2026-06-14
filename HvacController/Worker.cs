using HvacController.Services;

namespace HvacController;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ThermostatCycleRunner _cycleRunner;
    private readonly IRelayService _relays;

    public Worker(
        ILogger<Worker> logger,
        ThermostatCycleRunner cycleRunner,
        IRelayService relays)
    {
        _logger = logger;
        _cycleRunner = cycleRunner;
        _relays = relays;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HVAC controller starting.");

        _relays.AllOff();

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.Now;
            var output = _cycleRunner.RunOnce(now);

            _logger.LogInformation(
                "HVAC output: Heat={Heat}, Cool={Cool}, Fan={Fan}, Reason={Reason}",
                output.Heat,
                output.Cool,
                output.Fan,
                output.Reason);

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
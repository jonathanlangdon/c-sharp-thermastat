namespace HvacController.Models;

public sealed record HvacSettings
{
    public double TemperatureDifferentialF { get; init; } = 1.0;

    public TimeSpan MinimumCoolRunTime { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan MinimumCoolOffTime { get; init; } = TimeSpan.FromMinutes(5);

    public TimeSpan MinimumHeatRunTime { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan MinimumHeatOffTime { get; init; } = TimeSpan.FromMinutes(2);

    public TimeSpan SensorTimeout { get; init; } = TimeSpan.FromMinutes(3);

    public bool FanOnWithCooling { get; init; } = true;
    public bool FanOnWithHeat { get; init; } = false;
}

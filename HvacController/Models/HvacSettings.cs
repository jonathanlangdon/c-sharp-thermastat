namespace HvacController.Models;

public sealed record HvacSettings
{
    public double TemperatureDifferentialF { get; init; } = 1.0;

    public double AbsoluteHumidityCoolingThreshold { get; init; } = 9.0;

    public TimeSpan MinimumRunTime { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan MinimumOffTime { get; init; } = TimeSpan.FromMinutes(5);

    public TimeSpan SensorTimeout { get; init; } = TimeSpan.FromMinutes(3);

    public bool FanOnWithCooling { get; init; } = true;
    public bool FanOnWithHeat { get; init; } = false;
    public bool IdleFanOn { get; init; } = true;
}
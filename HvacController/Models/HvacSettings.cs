namespace HvacController.Models;

public sealed record HvacSettings
{
    public double TemperatureDifferentialF { get; init; } = 1.0;

    public double DayHeatSetPoint { get; init; } = 70.0;
    public double NightHeatSetPoint { get; init; } = 65.0;

    public TimeOnly NightHeatStart { get; init; } = new(19, 0);
    public TimeOnly NightHeatEnd { get; init; } = new(6, 0);

    public double AbsoluteHumidityCoolingOnThreshold { get; init; } = 9.5;
    public double AbsoluteHumidityCoolingOffThreshold { get; init; } = 9.0;

    public TimeSpan MinimumRunTime { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan MinimumOffTime { get; init; } = TimeSpan.FromMinutes(5);

    public TimeSpan SensorTimeout { get; init; } = TimeSpan.FromMinutes(3);
    
    public TimeSpan MotionSetPointHoldTime { get; init; } = TimeSpan.FromHours(2);

    public bool FanOnWithCooling { get; init; } = true;
    public bool FanOnWithHeat { get; init; } = false;
    public bool IdleFanOn { get; init; } = true;
}
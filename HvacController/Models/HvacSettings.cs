namespace HvacController.Models;

public sealed record HvacSettings
{
    public double TemperatureDifferentialF { get; init; } = 0.5;

    public double DayHeatSetPoint { get; init; } = 70.5;
    public double NightHeatSetPoint { get; init; } = 65.0;

    public TimeOnly NightHeatStart { get; init; } = new(19, 0);
    public TimeOnly NightHeatEnd { get; init; } = new(6, 0);

    public double OutdoorGoodHumidityHighestLevel { get; init; } = 10.4;

    public double AbsoluteHumidityCoolingOnThreshold { get; init; } = 11.0;
    public double AbsoluteHumidityCoolingOffThreshold { get; init; } = 10.5;

    public TimeSpan MinimumRunTime { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan MinimumOffTime { get; init; } = TimeSpan.FromMinutes(5);

    public TimeSpan SensorTimeout { get; init; } = TimeSpan.FromMinutes(3);

    public TimeSpan MotionSetPointHoldTime { get; init; } = TimeSpan.FromHours(1);

    public void ValidateFixedSettings()
    {
        if (MinimumRunTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MinimumRunTime),
                "Minimum run time must be greater than zero.");
        }

        if (MinimumOffTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MinimumOffTime),
                "Minimum off time must be greater than zero.");
        }

        if (SensorTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(SensorTimeout),
                "Sensor timeout must be greater than zero.");
        }
    }

    public bool HasValidCoolingThresholds()
    {
        return AbsoluteHumidityCoolingOnThreshold >
               AbsoluteHumidityCoolingOffThreshold;
    }
}
namespace HvacController.Models;

public sealed record HvacSettings
{
    public double TemperatureDifferentialF { get; init; } = 0.5;

    public TimeOnly NightHeatStart { get; init; } = new(19, 0);
    public TimeOnly NightHeatEnd { get; init; } = new(6, 0);

    public TimeSpan MinSafetyWindowTime { get; init; } = TimeSpan.FromMinutes(5);

    public TimeSpan SensorTimeout { get; init; } = TimeSpan.FromMinutes(3);

    public TimeSpan MotionSetPointHoldTime { get; init; } = TimeSpan.FromHours(1);

    public void ValidateFixedSettings()
    {
        if (MinSafetyWindowTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MinSafetyWindowTime),
                "Minimum run time must be greater than zero.");
        }

        if (SensorTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(SensorTimeout),
                "Sensor timeout must be greater than zero.");
        }
    }
}
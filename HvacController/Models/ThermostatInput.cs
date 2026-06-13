namespace HvacController.Models;

public sealed record ThermostatInput
{
    public double? CurrentTempF { get; init; }
    public double? CurrentHumidity { get; init; }

    public HvacMode Mode { get; init; } = HvacMode.Heat;

    public DateTimeOffset Now { get; init; }
    public DateTimeOffset? LastSensorUpdate { get; init; }
    public DateTimeOffset? LastMotionDetected { get; init; }
}
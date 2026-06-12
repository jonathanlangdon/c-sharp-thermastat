namespace HvacController.Models;

public sealed record ThermostatInput
{
    public double? CurrentTempF { get; init; }
    public double? CurrentHumidity { get; init; }

    public double SetpointF { get; init; }
    public HvacMode Mode { get; init; } = HvacMode.Cool;

    public DateTimeOffset Now { get; init; }
    public DateTimeOffset? LastSensorUpdate { get; init; }
}
namespace HvacController.Models;

public sealed record ThermostatInput
{
    public double? CurrentTempF { get; init; }
    public double SetpointF { get; init; }
    public HvacMode Mode { get; init; } = HvacMode.Off;
    public bool FanAlwaysOn { get; init; }
    public DateTimeOffset Now { get; init; }
    public DateTimeOffset? LastSensorUpdate { get; init; }
}

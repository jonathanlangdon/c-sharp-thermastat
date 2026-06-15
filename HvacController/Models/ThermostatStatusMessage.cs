namespace HvacController.Models;

public sealed record ThermostatStatusMessage
{
    public DateTimeOffset Now { get; init; }
    public DateTimeOffset? LastSensorUpdate { get; init; }
    public DateTimeOffset? LastMotionDetected { get; init; }
    public DateTimeOffset? OutsideWeatherUpdatedAt { get; init; }

    public bool Heat { get; init; }
    public bool Cool { get; init; }
    public bool Fan { get; init; }
    public string Reason { get; init; } = "";

    public HvacMode Mode { get; init; }

    public double? UpstairsTemperature { get; init; }
    public double? UpstairsRelativeHumidity { get; init; }
    public double? UpstairsAbsoluteHumidity { get; init; }

    public double? DownstairsTemperature { get; init; }
    public double? DownstairsRelativeHumidity { get; init; }
    public double? DownstairsAbsoluteHumidity { get; init; }

    public double? ControlAbsoluteHumidity { get; init; }

    public double? OutsideTemperature { get; init; }
    public double? OutsideAbsoluteHumidity { get; init; }

    public bool WasHeating { get; init; }
    public bool WasCooling { get; init; }
    public DateTimeOffset? LastHeatStarted { get; init; }
    public DateTimeOffset? LastHeatStopped { get; init; }
    public DateTimeOffset? LastCoolStarted { get; init; }
    public DateTimeOffset? LastCoolStopped { get; init; }
}

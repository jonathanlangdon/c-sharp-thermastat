namespace HvacController.Models;

public sealed record ThermostatRuntimeState
{
    public bool WasHeating { get; init; }
    public bool WasCooling { get; init; }
    public DateTimeOffset? LastHeatStarted { get; init; }
    public DateTimeOffset? LastHeatStopped { get; init; }
    public DateTimeOffset? LastCoolStarted { get; init; }
    public DateTimeOffset? LastCoolStopped { get; init; }

    public static ThermostatRuntimeState Empty { get; } = new();
}

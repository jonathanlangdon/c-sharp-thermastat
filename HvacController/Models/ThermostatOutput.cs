namespace HvacController.Models;

public sealed record ThermostatOutput
{
    public bool Heat { get; init; }
    public bool Cool { get; init; }
    public bool Fan { get; init; }
    public string Reason { get; init; } = "";
}

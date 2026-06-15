namespace HvacController.Models;

public sealed record AppSettings
{
    public string RunStatus { get; init; } = "dev";
}

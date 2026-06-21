namespace HvacController.Models;

public sealed record ModeSetMessage
{
    public string? Mode { get; init; }
}
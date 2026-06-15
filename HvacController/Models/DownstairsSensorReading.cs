namespace HvacController.Models;

public sealed record DownstairsSensorReading
{
    public double TemperatureFahr { get; init; }
    public double RelativeHumidity { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

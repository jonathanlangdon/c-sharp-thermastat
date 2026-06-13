namespace HvacController.Models;

public sealed record OutsideWeatherReading
{
    public double OutsideTemperature { get; init; }
    public double OutsideAbsoluteHumidity { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
namespace HvacController.Models;

public sealed record UpstairsSensorMessage
{
    public double TemperatureFahr { get; init; }
    public double RelativeHumidity { get; init; }

    public bool? MotionDetected { get; init; }
    public HvacMode? Mode { get; init; }
}

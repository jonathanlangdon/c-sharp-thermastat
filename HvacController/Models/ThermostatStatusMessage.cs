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
    public double? HeatSetPointFahr { get; init; }
    public double HeatSetPointDay { get; init; }
    public double HeatSetPointNight { get; init; }
    
    public double HumidityTargetIdeal { get; init; }
    public double HumidityTargetGood { get; init; }
    public double HumidityTargetFair { get; init; }

    public double MaxAbsHumSetPoint { get; init; }

    public bool ManualOverride { get; init; }
    public ManualMode ManualMode { get; init; }


    public double UpTempCalibration { get; init; }
    public double UpRelHumCalibration { get; init; }
    public double DownTempCalibration { get; init; }
    public double DownRelHumCalibration { get; init; }
    
    public double? UpstairsTemperature { get; init; }
    public double? UpstairsRelativeHumidity { get; init; }
    public double? UpstairsAbsoluteHumidity { get; init; }

    public double? DownstairsTemperature { get; init; }
    public double? DownstairsRelativeHumidity { get; init; }
    public double? DownstairsAbsoluteHumidity { get; init; }

    public double? ControlAbsoluteHumidity { get; init; }
    public bool ShouldOpenWindows { get; init; }
    public double? DehumidSetUp { get; init; }
    public double? DehumidSetDown { get; init; }

    public double? OutsideTemperature { get; init; }
    public double? OutsideAbsoluteHumidity { get; init; }

    public bool WasHeating { get; init; }
    public bool WasCooling { get; init; }

    public double CoolHoursToday { get; init; }
    public double HeatHoursToday { get; init; }

    public DateTimeOffset? LastHeatStarted { get; init; }
    public DateTimeOffset? LastHeatStopped { get; init; }
    public DateTimeOffset? LastCoolStarted { get; init; }
    public DateTimeOffset? LastCoolStopped { get; init; }
}

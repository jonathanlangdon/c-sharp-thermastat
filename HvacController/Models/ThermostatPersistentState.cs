namespace HvacController.Models;

public sealed record ThermostatPersistentState
{
    public DateTimeOffset? LastSensorUpdate { get; init; }
    public DateTimeOffset? LastMotionDetected { get; init; }

    public HvacMode Mode { get; init; } = HvacMode.Heat;

    public double HeatSetPointDay { get; init; } = 70.5;
    public double HeatSetPointNight { get; init; } = 65.0;
    public double MaxAbsHumSetPoint { get; init; } = 10.5;
    // only if less than 71,... otherwise 10 for 71-72 and 9 for >= 72
    public double UpTempCalibration { get; init; } = 4.0;
    public double UpRelHumCalibration { get; init; } = 0.0;
    public double DownTempCalibration { get; init; } = 0.0;
    public double DownRelHumCalibration { get; init; } = -2.0;

    public bool WasHeating { get; init; }
    public bool WasCooling { get; init; }

    public DateTimeOffset? LastHeatStarted { get; init; }
    public DateTimeOffset? LastHeatStopped { get; init; }
    public DateTimeOffset? LastCoolStarted { get; init; }
    public DateTimeOffset? LastCoolStopped { get; init; }

    public ThermostatRuntimeState ToRuntimeState()
    {
        return new ThermostatRuntimeState
        {
            WasHeating = WasHeating,
            WasCooling = WasCooling,
            LastHeatStarted = LastHeatStarted,
            LastHeatStopped = LastHeatStopped,
            LastCoolStarted = LastCoolStarted,
            LastCoolStopped = LastCoolStopped
        };
    }

    public static ThermostatPersistentState From(
        ThermostatInput input,
        ThermostatRuntimeState runtimeState)
    {
        return new ThermostatPersistentState
        {
            LastSensorUpdate = input.LastSensorUpdate,
            LastMotionDetected = input.LastMotionDetected,
            Mode = input.Mode,

            HeatSetPointDay = input.DayHeatSetPoint,
            HeatSetPointNight = input.NightHeatSetPoint,
            MaxAbsHumSetPoint = input.AbsoluteHumidityCoolingOnThreshold,

            UpTempCalibration = input.UpTempCalibration,
            UpRelHumCalibration = input.UpRelHumCalibration,
            DownTempCalibration = input.DownTempCalibration,
            DownRelHumCalibration = input.DownRelHumCalibration,

            WasHeating = runtimeState.WasHeating,
            WasCooling = runtimeState.WasCooling,
            LastHeatStarted = runtimeState.LastHeatStarted,
            LastHeatStopped = runtimeState.LastHeatStopped,
            LastCoolStarted = runtimeState.LastCoolStarted,
            LastCoolStopped = runtimeState.LastCoolStopped
        };
    }
}
namespace HvacController.Models;

public sealed record ThermostatPersistentState
{
    public DateTimeOffset? LastSensorUpdate { get; init; }
    public DateTimeOffset? LastMotionDetected { get; init; }

    public HvacMode Mode { get; init; } = HvacMode.Heat;

    public double HeatSetPointDay { get; init; } = 70;
    public double HeatSetPointNight { get; init; } = 65.0;

    public double HumidityTargetIdeal { get; init; } = 9.9;
    public double HumidityTargetGood { get; init; } = 10.4;
    public double HumidityTargetFair { get; init; } = 10.9;

    public bool ManualOverride { get; init; }
    public ManualMode ManualMode { get; init; } = ManualMode.Off;

    public double UpTempCalibration { get; init; } = -4.5;
    public double UpRelHumCalibration { get; init; } = 3.0;
    public double DownTempCalibration { get; init; } = -.5;
    public double DownRelHumCalibration { get; init; } = -1.5;

    public bool WasHeating { get; init; }
    public bool WasCooling { get; init; }

    public DateOnly RuntimeDate { get; init; }
    public double CoolHoursToday { get; init; }
    public double HeatHoursToday { get; init; }
    public DateTimeOffset? LastRuntimeUpdated { get; init; }

    public DateTimeOffset? LastHeatStarted { get; init; }
    public DateTimeOffset? LastHeatStopped { get; init; }
    public DateTimeOffset? LastCoolStarted { get; init; }
    public DateTimeOffset? LastCoolStopped { get; init; }

    public ThermostatPersistentState UpdateRuntimeTotals(
    DateTimeOffset now,
    ThermostatRuntimeState previousRuntimeState)
    {
        var today = DateOnly.FromDateTime(now.LocalDateTime);

        var coolHoursToday = RuntimeDate == today
            ? CoolHoursToday
            : 0.0;

        var heatHoursToday = RuntimeDate == today
            ? HeatHoursToday
            : 0.0;

        if (LastRuntimeUpdated is not null &&
            now > LastRuntimeUpdated.Value)
        {
            var elapsed = now - LastRuntimeUpdated.Value;

            // Prevent a reboot/service outage from adding several fake hours.
            if (elapsed <= TimeSpan.FromMinutes(2))
            {
                var todayStart = new DateTimeOffset(
                    today.ToDateTime(TimeOnly.MinValue),
                    now.Offset);

                var start = LastRuntimeUpdated.Value > todayStart
                    ? LastRuntimeUpdated.Value
                    : todayStart;

                if (now > start)
                {
                    var elapsedHours = (now - start).TotalHours;

                    if (previousRuntimeState.WasCooling)
                    {
                        coolHoursToday += elapsedHours;
                    }

                    if (previousRuntimeState.WasHeating)
                    {
                        heatHoursToday += elapsedHours;
                    }
                }
            }
        }

        return this with
        {
            RuntimeDate = today,
            CoolHoursToday = coolHoursToday,
            HeatHoursToday = heatHoursToday,
            LastRuntimeUpdated = now
        };
    }

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

            HumidityTargetIdeal = input.HumidityTargetIdeal,
            HumidityTargetGood = input.HumidityTargetGood,
            HumidityTargetFair = input.HumidityTargetFair,

            ManualOverride = input.ManualOverride,
            ManualMode = input.ManualMode,

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
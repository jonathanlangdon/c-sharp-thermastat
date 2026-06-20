using HvacController.Models;

namespace HvacController.Services;

public sealed class ThermostatEngine
{
    private const string NormalReason = "Everything Normal";
    private const string NoTemperatureReason = "No temperature reading";
    private const string NoHumidityReason = "No humidity reading";
    private const string SensorTimeoutReason = "Sensor timeout";
    private const string OutdoorHumidityUnavailableReason = "Outdoor humidity is unavailable";

    private readonly HvacSettings _settings;

    public ThermostatEngine(HvacSettings settings)
    {
        settings.ValidateFixedSettings();

        _settings = settings;
    }

    public (ThermostatOutput Output, ThermostatRuntimeState State) Evaluate(
        ThermostatInput input,
        ThermostatRuntimeState previousState)
    {
        if (input.CurrentTempFahrUp is null)
        {
            return SafeOff(previousState, input.Now, NoTemperatureReason);
        }

        if (input.ControlHumidity is null)
        {
            return SafeOff(previousState, input.Now, NoHumidityReason);
        }

        if (input.LastSensorUpdate is null ||
            input.Now - input.LastSensorUpdate > _settings.SensorTimeout)
        {
            return SafeOff(previousState, input.Now, SensorTimeoutReason);
        }

        var temp = input.CurrentTempFahrUp.Value;
        var absoluteHumidity = input.ControlHumidity.Value;
        var heatSetPoint = GetHeatSetPoint(input);

        return input.Mode switch
        {
            HvacMode.Heat => Transition(
                previousState,
                input.Now,
                CreateOutput(
                    heat: ShouldHeat(input, previousState, temp, heatSetPoint),
                    cool: false,
                    fan: true,
                    heatSetPoint,
                    NormalReason)),

            HvacMode.Cool when input.OutsideAbsoluteHumidity is null => Transition(
                previousState,
                input.Now,
                CreateOutput(
                    heat: false,
                    cool: false,
                    fan: true,
                    heatSetPoint,
                    OutdoorHumidityUnavailableReason)),

            HvacMode.Cool => Transition(
                previousState,
                input.Now,
                CreateOutput(
                    heat: false,
                    cool: ShouldCool(input, previousState, absoluteHumidity),
                    fan: true,
                    heatSetPoint,
                    NormalReason)),

            _ => SafeOff(previousState, input.Now, $"Unsupported HVAC mode: {input.Mode}")
        };
    }

    private double GetHeatSetPoint(ThermostatInput input)
    {
        if (IsNightTime(input.Now))
        {
            return _settings.NightHeatSetPoint;
        }

        return HasRecentMotion(input)
            ? _settings.DayHeatSetPoint
            : _settings.NightHeatSetPoint;
    }

    private bool IsNightTime(DateTimeOffset now)
    {
        var currentTime = TimeOnly.FromDateTime(now.LocalDateTime);

        return _settings.NightHeatStart > _settings.NightHeatEnd
            ? currentTime >= _settings.NightHeatStart ||
              currentTime < _settings.NightHeatEnd
            : currentTime >= _settings.NightHeatStart &&
              currentTime < _settings.NightHeatEnd;
    }

    private bool HasRecentMotion(ThermostatInput input)
    {
        return input.LastMotionDetected is not null &&
               input.Now - input.LastMotionDetected <= _settings.MotionSetPointHoldTime;
    }

    private bool ShouldHeat(
        ThermostatInput input,
        ThermostatRuntimeState state,
        double temp,
        double heatSetPoint)
    {
        if (state.WasHeating)
        {
            if (!MinimumRunSatisfied(input.Now, state.LastHeatStarted))
            {
                return true;
            }

            return temp < heatSetPoint;
        }

        return MinimumOffSatisfied(input.Now, state.LastHeatStopped) &&
               temp <= heatSetPoint - _settings.TemperatureDifferentialF;
    }

    private bool ShouldCool(
        ThermostatInput input,
        ThermostatRuntimeState state,
        double absoluteHumidity)
    {
        if (state.WasCooling)
        {
            if (!MinimumRunSatisfied(input.Now, state.LastCoolStarted))
            {
                return true;
            }

            return absoluteHumidity > _settings.AbsoluteHumidityCoolingOffThreshold;
        }

        return MinimumOffSatisfied(input.Now, state.LastCoolStopped) &&
               TooHumidInsideAndOut(input, absoluteHumidity);
    }

    private bool TooHumidInsideAndOut(
        ThermostatInput input,
        double absoluteHumidity)
    {
        return input.OutsideAbsoluteHumidity is not null &&
               input.OutsideAbsoluteHumidity > _settings.OutdoorGoodHumidityHighestLevel &&
               absoluteHumidity >= _settings.AbsoluteHumidityCoolingOnThreshold;
    }

    private bool MinimumRunSatisfied(
        DateTimeOffset now,
        DateTimeOffset? startedAt)
    {
        return startedAt is null ||
               now - startedAt >= _settings.MinimumRunTime;
    }

    private bool MinimumOffSatisfied(
        DateTimeOffset now,
        DateTimeOffset? stoppedAt)
    {
        return stoppedAt is null ||
               now - stoppedAt >= _settings.MinimumOffTime;
    }

    private static ThermostatOutput CreateOutput(
        bool heat,
        bool cool,
        bool fan,
        double? heatSetPoint,
        string reason)
    {
        return new ThermostatOutput
        {
            Heat = heat,
            Cool = cool,
            Fan = fan,
            HeatSetPointFahr = heatSetPoint,
            Reason = reason
        };
    }

    private static (ThermostatOutput Output, ThermostatRuntimeState State) SafeOff(
        ThermostatRuntimeState previousState,
        DateTimeOffset now,
        string reason)
    {
        return Transition(previousState, now, new ThermostatOutput
        {
            Heat = false,
            Cool = false,
            Fan = false,
            Reason = reason
        });
    }

    private static (ThermostatOutput Output, ThermostatRuntimeState State) Transition(
        ThermostatRuntimeState previousState,
        DateTimeOffset now,
        ThermostatOutput output)
    {
        var state = previousState;

        if (!previousState.WasHeating && output.Heat)
        {
            state = state with { LastHeatStarted = now };
        }

        if (previousState.WasHeating && !output.Heat)
        {
            state = state with { LastHeatStopped = now };
        }

        if (!previousState.WasCooling && output.Cool)
        {
            state = state with { LastCoolStarted = now };
        }

        if (previousState.WasCooling && !output.Cool)
        {
            state = state with { LastCoolStopped = now };
        }

        state = state with
        {
            WasHeating = output.Heat,
            WasCooling = output.Cool
        };

        return (output, state);
    }
}
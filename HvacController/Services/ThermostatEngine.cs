using HvacController.Models;

namespace HvacController.Services;

public sealed class ThermostatEngine
{
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
            return SafeOff(previousState, input.Now, "No temperature reading");
        }

        if (input.ControlHumidity is null)
        {
            return SafeOff(previousState, input.Now, "No humidity reading");
        }

        if (input.LastSensorUpdate is null ||
            input.Now - input.LastSensorUpdate > _settings.SensorTimeout)
        {
            return SafeOff(previousState, input.Now, "Sensor timeout");
        }

        var temp = input.CurrentTempFahrUp.Value;
        var absoluteHumidity = input.ControlHumidity.Value;
        var heatSetPoint = GetHeatSetPoint(input);

        if (input.Mode == HvacMode.Cool &&
            input.OutsideAbsoluteHumidity is null)
        {
            return Transition(previousState, input.Now, new ThermostatOutput
            {
                Heat = false,
                Cool = false,
                Fan = true,
                HeatSetPointFahr = heatSetPoint,
                Reason = "Outdoor humidity is unavailable"
            });
        }

        var heat = input.Mode == HvacMode.Heat
            && ShouldHeat(input, previousState, temp, heatSetPoint);

        var cool = input.Mode == HvacMode.Cool
            && MoreHumidOutside(input, absoluteHumidity)
            && ShouldCool(input, previousState, absoluteHumidity);

        var fan = true;

        // Hard safety rule.
        if (heat && cool)
        {
            heat = false;
            cool = false;
            fan = false;
        }

        return Transition(previousState, input.Now, new ThermostatOutput
        {
            Heat = heat,
            Cool = cool,
            Fan = fan,
            HeatSetPointFahr = heatSetPoint,
            Reason = "Everything Normal"
        });
    }

    private double GetHeatSetPoint(ThermostatInput input)
    {
        var currentTime = TimeOnly.FromDateTime(input.Now.LocalDateTime);

        var isNightTime = _settings.NightHeatStart > _settings.NightHeatEnd
            ? currentTime >= _settings.NightHeatStart ||
            currentTime < _settings.NightHeatEnd
            : currentTime >= _settings.NightHeatStart &&
            currentTime < _settings.NightHeatEnd;

        if (isNightTime)
        {
            return _settings.NightHeatSetPoint;
        }

        var hasRecentMotion =
            input.LastMotionDetected is not null &&
            input.Now - input.LastMotionDetected <= _settings.MotionSetPointHoldTime;

        return hasRecentMotion
            ? _settings.DayHeatSetPoint
            : _settings.NightHeatSetPoint;
    }

    private bool ShouldHeat(
        ThermostatInput input,
        ThermostatRuntimeState state,
        double temp,
        double heatSetPoint)
    {
        if (state.WasHeating)
        {
            var minRunSatisfied =
                state.LastHeatStarted is null ||
                input.Now - state.LastHeatStarted >= _settings.MinimumRunTime;

            if (!minRunSatisfied)
            {
                return true;
            }

            return temp < heatSetPoint;
        }

        var minOffSatisfied =
            state.LastHeatStopped is null ||
            input.Now - state.LastHeatStopped >= _settings.MinimumOffTime;

        return minOffSatisfied &&
               temp <= heatSetPoint - _settings.TemperatureDifferentialF;
    }

    private static bool MoreHumidOutside(
        ThermostatInput input,
        double absoluteHumidity)
    {
        return input.OutsideAbsoluteHumidity is not null &&
            input.OutsideAbsoluteHumidity > absoluteHumidity;
    }

    private bool ShouldCool(
        ThermostatInput input,
        ThermostatRuntimeState state,
        double absoluteHumidity)
    {
        if (state.WasCooling)
        {
            var minRunSatisfied =
                state.LastCoolStarted is null ||
                input.Now - state.LastCoolStarted >= _settings.MinimumRunTime;

            if (!minRunSatisfied)
            {
                return true;
            }

            return absoluteHumidity > _settings.AbsoluteHumidityCoolingOffThreshold;
        }

        var minOffSatisfied =
            state.LastCoolStopped is null ||
            input.Now - state.LastCoolStopped >= _settings.MinimumOffTime;

        return minOffSatisfied &&
            absoluteHumidity >= _settings.AbsoluteHumidityCoolingOnThreshold;
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
using HvacController.Models;

namespace HvacController.Services;

public sealed class ThermostatEngine
{
    private readonly HvacSettings _settings;

    public ThermostatEngine(HvacSettings settings)
    {
        _settings = settings;
    }

    public (ThermostatOutput Output, ThermostatRuntimeState State) Evaluate(
        ThermostatInput input,
        ThermostatRuntimeState previousState)
    {
        if (input.CurrentTempF is null)
        {
            return SafeOff(previousState, input.Now, "No temperature reading");
        }

        if (input.CurrentHumidity is null)
        {
            return SafeOff(previousState, input.Now, "No humidity reading");
        }

        if (input.LastSensorUpdate is null ||
            input.Now - input.LastSensorUpdate > _settings.SensorTimeout)
        {
            return SafeOff(previousState, input.Now, "Sensor timeout");
        }

        var temp = input.CurrentTempF.Value;
        var relativeHumidity = input.CurrentHumidity.Value;
        var absoluteHumidity =
            AbsoluteHumidityCalculator.CalculateGramsPerCubicMeterFromFahrenheit(
                temp,
                relativeHumidity);

        var heat = input.Mode == HvacMode.Heat
            && ShouldHeat(input, previousState, temp);

        var cool = input.Mode == HvacMode.Cool
            && ShouldCool(input, previousState, absoluteHumidity);

        var fan = false;

        // Hard safety rule.
        if (heat && cool)
        {
            heat = false;
            cool = false;
            fan = false;
        }

        if (cool && _settings.FanOnWithCooling)
        {
            fan = true;
        }

        if (heat && _settings.FanOnWithHeat)
        {
            fan = true;
        }

        if (!heat && !cool && _settings.IdleFanOn)
        {
            fan = true;
        }

        return Transition(previousState, input.Now, new ThermostatOutput
        {
            Heat = heat,
            Cool = cool,
            Fan = fan,
            Reason = "Everything Normal"
        });
    }

    private bool ShouldHeat(
        ThermostatInput input,
        ThermostatRuntimeState state,
        double temp)
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

            return temp < input.SetpointF;
        }

        var minOffSatisfied =
            state.LastHeatStopped is null ||
            input.Now - state.LastHeatStopped >= _settings.MinimumOffTime;

        return minOffSatisfied &&
               temp <= input.SetpointF - _settings.TemperatureDifferentialF;
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
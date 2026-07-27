using HvacController.Models;
using HvacController.Services;

namespace HvacController.Tests;

public sealed class ThermostatEngineManualOverrideTests
{
    [Fact]
    public void Evaluate_WhenManualOverrideIsOnAndManualModeIsOff_TurnsEverythingOff()
    {
        var now = TestTime();
        var engine = new ThermostatEngine(new HvacSettings());

        var input = TestInput(now) with
        {
            Mode = HvacMode.Cool,
            ManualOverride = true,
            ManualMode = ManualMode.Off
        };

        var previousState = ThermostatRuntimeState.Empty with
        {
            WasCooling = true,
            LastCoolStarted = now - TimeSpan.FromMinutes(10)
        };

        var (output, state) = engine.Evaluate(input, previousState);

        Assert.False(output.Heat);
        Assert.False(output.Cool);
        Assert.False(output.Fan);
        Assert.Equal("Manual override", output.Reason);

        Assert.False(state.WasHeating);
        Assert.False(state.WasCooling);
        Assert.Equal(now, state.LastCoolStopped);
    }

    [Fact]
    public void Evaluate_WhenManualOverrideIsOnAndManualModeIsFan_TurnsFanOnOnly()
    {
        var now = TestTime();
        var engine = new ThermostatEngine(new HvacSettings());

        var input = TestInput(now) with
        {
            Mode = HvacMode.Heat,
            CurrentTempFahrUp = 60.0,
            ManualOverride = true,
            ManualMode = ManualMode.Fan
        };

        var (output, state) = engine.Evaluate(
            input,
            ThermostatRuntimeState.Empty);

        Assert.False(output.Heat);
        Assert.False(output.Cool);
        Assert.True(output.Fan);
        Assert.Equal("Manual override", output.Reason);

        Assert.False(state.WasHeating);
        Assert.False(state.WasCooling);
    }

    [Fact]
    public void Evaluate_WhenManualOverrideIsOn_IgnoresMissingSensorReadings()
    {
        var now = TestTime();
        var engine = new ThermostatEngine(new HvacSettings());

        var input = TestInput(now) with
        {
            CurrentTempFahrUp = null,
            RelHumidityUpstairs = null,
            LastSensorUpdate = null,
            ManualOverride = true,
            ManualMode = ManualMode.Fan
        };

        var (output, _) = engine.Evaluate(
            input,
            ThermostatRuntimeState.Empty);

        Assert.False(output.Heat);
        Assert.False(output.Cool);
        Assert.True(output.Fan);
        Assert.Equal("Manual override", output.Reason);
    }

    private static ThermostatInput TestInput(DateTimeOffset now)
    {
        return new ThermostatInput
        {
            CurrentTempFahrUp = 72.0,
            RelHumidityUpstairs = 50.0,
            OutsideAbsoluteHumidity = 12.0,

            Mode = HvacMode.Cool,

            DayHeatSetPoint = 70.5,
            NightHeatSetPoint = 65.0,

            HumidityTargetIdeal = 9.0,
            HumidityTargetGood = 10.0,
            HumidityTargetFair = 11.0,

            ManualOverride = false,
            ManualMode = ManualMode.Off,

            Now = now,
            LastSensorUpdate = now,
            LastMotionDetected = now
        };
    }

    private static DateTimeOffset TestTime()
    {
        return new DateTimeOffset(
            2026, 7, 27, 13, 0, 0,
            TimeSpan.FromHours(-4));
    }
}

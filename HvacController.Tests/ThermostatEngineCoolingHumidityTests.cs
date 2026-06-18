using HvacController.Models;
using HvacController.Services;

namespace HvacController.Tests;

public sealed class ThermostatEngineCoolingHumidityTests
{
    [Fact]
    public void Evaluate_CoolsWhenOutdoorHumidityIsUnknownAndCoolingThresholdIsMet()
    {
        var now = TestTime();
        var engine = new ThermostatEngine(new HvacSettings());

        var input = CoolInput(
            now,
            outsideAbsoluteHumidity: null);

        var (output, _) = engine.Evaluate(
            input,
            ThermostatRuntimeState.Empty);

        Assert.False(output.Cool);
        Assert.False(output.Heat);
        Assert.True(output.Fan);
        Assert.Equal("Outdoor humidity is unavailable", output.Reason);
    }

    [Fact]
    public void Evaluate_CoolsWhenOutsideIsMoreHumidThanControlHumidityAndCoolingThresholdIsMet()
    {
        var now = TestTime();
        var engine = new ThermostatEngine(new HvacSettings());

        var input = CoolInput(now);
        var controlHumidity = input.ControlHumidity!.Value;

        input = input with
        {
            OutsideAbsoluteHumidity = controlHumidity + 1
        };

        var (output, _) = engine.Evaluate(
            input,
            ThermostatRuntimeState.Empty);

        Assert.True(output.Cool);
        Assert.False(output.Heat);
        Assert.True(output.Fan);
        Assert.Equal("Everything Normal", output.Reason);
        Assert.True(
            input.ControlHumidity >= new HvacSettings().AbsoluteHumidityCoolingOnThreshold);
    }

    [Fact]
    public void Evaluate_DoesNotCoolWhenOutdoorHumidityIsUnknownAndCoolingThresholdIsMet()
    {
        var now = TestTime();
        var engine = new ThermostatEngine(new HvacSettings());

        var input = CoolInput(
            now,
            outsideAbsoluteHumidity: null);

        var (output, _) = engine.Evaluate(
            input,
            ThermostatRuntimeState.Empty);

        Assert.False(output.Cool);
        Assert.False(output.Heat);
        Assert.True(output.Fan);
        Assert.Equal("Outdoor humidity is unavailable", output.Reason);
    }

    [Fact]
    public void Evaluate_ReturnsNoHumidityReadingWhenControlHumidityIsMissingEvenIfOutsideHumidityIsUnavailable()
    {
        var now = TestTime();
        var engine = new ThermostatEngine(new HvacSettings());

        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 72.0,
            HumidityUpstairs = null,
            Mode = HvacMode.Cool,
            OutsideAbsoluteHumidity = null,
            Now = now,
            LastSensorUpdate = now
        };

        Assert.Null(input.ControlHumidity);

        var (output, _) = engine.Evaluate(
            input,
            ThermostatRuntimeState.Empty);

        Assert.False(output.Cool);
        Assert.False(output.Heat);
        Assert.False(output.Fan);
        Assert.Equal("No humidity reading", output.Reason);
    }

    [Fact]
    public void Evaluate_DoesNotCoolWhenOutsideHumidityEqualsControlHumidityEvenIfCoolingThresholdIsMet()
    {
        var now = TestTime();
        var engine = new ThermostatEngine(new HvacSettings());

        var input = CoolInput(now);
        var controlHumidity = input.ControlHumidity!.Value;

        input = input with
        {
            OutsideAbsoluteHumidity = controlHumidity
        };

        var (output, _) = engine.Evaluate(
            input,
            ThermostatRuntimeState.Empty);

        Assert.False(output.Cool);
        Assert.False(output.Heat);
        Assert.True(output.Fan);
        Assert.Equal("Everything Normal", output.Reason);
    }

    [Fact]
    public void Evaluate_DoesNotCoolWhenOutsideHumidityIsLowerThanControlHumidityEvenIfCoolingThresholdIsMet()
    {
        var now = TestTime();
        var engine = new ThermostatEngine(new HvacSettings());

        var input = CoolInput(now);
        var controlHumidity = input.ControlHumidity!.Value;

        input = input with
        {
            OutsideAbsoluteHumidity = controlHumidity - 0.01
        };

        var (output, _) = engine.Evaluate(
            input,
            ThermostatRuntimeState.Empty);

        Assert.False(output.Cool);
        Assert.False(output.Heat);
        Assert.True(output.Fan);
        Assert.Equal("Everything Normal", output.Reason);
    }

    [Fact]
    public void Evaluate_DoesNotCoolWhenCoolingThresholdIsNotMetEvenIfOutsideIsMoreHumid()
    {
        var now = TestTime();
        var settings = new HvacSettings();
        var engine = new ThermostatEngine(settings);

        var input = CoolInput(
            now,
            temperatureFahr: 65.0,
            relativeHumidity: 40.0,
            outsideAbsoluteHumidity: 12.0);

        Assert.True(
            input.ControlHumidity < settings.AbsoluteHumidityCoolingOnThreshold);

        var (output, _) = engine.Evaluate(
            input,
            ThermostatRuntimeState.Empty);

        Assert.False(output.Cool);
        Assert.False(output.Heat);
        Assert.True(output.Fan);
        Assert.Equal("Everything Normal", output.Reason);
    }

    [Fact]
    public void Evaluate_ReturnsSafeOffWhenControlHumidityIsMissingEvenIfCoolingWasAlreadyRunning()
    {
        var now = TestTime();
        var engine = new ThermostatEngine(new HvacSettings());

        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 72.0,
            HumidityUpstairs = null,
            Mode = HvacMode.Cool,
            OutsideAbsoluteHumidity = 12.0,
            Now = now,
            LastSensorUpdate = now
        };

        Assert.Null(input.ControlHumidity);

        var previousState = new ThermostatRuntimeState
        {
            WasCooling = true,
            LastCoolStarted = now - TimeSpan.FromMinutes(1)
        };

        var (output, state) = engine.Evaluate(
            input,
            previousState);

        Assert.False(output.Cool);
        Assert.False(output.Heat);
        Assert.False(output.Fan);
        Assert.Equal("No humidity reading", output.Reason);

        Assert.False(state.WasCooling);
        Assert.Equal(now, state.LastCoolStopped);
    }

    private static ThermostatInput CoolInput(
        DateTimeOffset now,
        double temperatureFahr = 72.0,
        double relativeHumidity = 60.0,
        double? outsideAbsoluteHumidity = 14.0)
    {
        return new ThermostatInput
        {
            CurrentTempFahrUp = temperatureFahr,
            HumidityUpstairs = relativeHumidity,
            Mode = HvacMode.Cool,
            OutsideAbsoluteHumidity = outsideAbsoluteHumidity,
            Now = now,
            LastSensorUpdate = now
        };
    }

    private static DateTimeOffset TestTime()
    {
        return new DateTimeOffset(
            2026, 6, 16, 14, 0, 0,
            TimeSpan.FromHours(-4));
    }
}

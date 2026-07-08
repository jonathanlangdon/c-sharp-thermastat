using HvacController.Models;
using HvacController.Services;

namespace HvacController.Tests;

public sealed class ThermostatEngineCoolingHumidityTests
{
    [Fact]
    public void Evaluate_DoesNotCoolWhenOutdoorHumidityIsUnknownAndCoolingThresholdIsMet()
    {
        var now = TestTime();
        var engine = new ThermostatEngine(new HvacSettings());

        var input = CoolInput(
            now,
            relativeHumidity: 60.0,
            outsideAbsoluteHumidity: null,
            maxAbsHumSetPoint: 10.8);

        var (output, _) = engine.Evaluate(
            input,
            ThermostatRuntimeState.Empty);

        Assert.False(output.Cool);
        Assert.False(output.Heat);
        Assert.True(output.Fan);
        Assert.Equal("Outdoor humidity is unavailable", output.Reason);
    }

    [Fact]
    public void Evaluate_CoolsWhenCoolingThresholdIsMet()
    {
        var now = TestTime();
        var engine = new ThermostatEngine(new HvacSettings());

        var input = CoolInput(
            now,
            temperatureFahr: 72.0,
            relativeHumidity: 60.0,
            outsideAbsoluteHumidity: 14.0,
            maxAbsHumSetPoint: 10.8);

        Assert.True(input.ControlHumidity >= input.AbsoluteHumidityCoolingOnThreshold);

        var (output, _) = engine.Evaluate(
            input,
            ThermostatRuntimeState.Empty);

        Assert.True(output.Cool);
        Assert.False(output.Heat);
        Assert.True(output.Fan);
        Assert.Equal("Everything Normal", output.Reason);
    }

    [Fact]
    public void Evaluate_DoesNotCoolWhenCoolingThresholdIsNotMet()
    {
        var now = TestTime();
        var engine = new ThermostatEngine(new HvacSettings());

        var input = CoolInput(
            now,
            temperatureFahr: 65.0,
            relativeHumidity: 40.0,
            outsideAbsoluteHumidity: 12.0,
            maxAbsHumSetPoint: 10.8);

        Assert.True(input.ControlHumidity < input.AbsoluteHumidityCoolingOnThreshold);

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
            RelHumidityUpstairs = null,
            Mode = HvacMode.Cool,
            OutsideAbsoluteHumidity = 12.0,
            AbsoluteHumidityCoolingOnThreshold = 10.8,
            DayHeatSetPoint = 70.5,
            NightHeatSetPoint = 65.0,
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

    [Fact]
    public void Evaluate_CoolingAlreadyRunning_StaysOnUntilMinSafetyWindowIsSatisfied()
    {
        var now = TestTime();
        var engine = new ThermostatEngine(new HvacSettings
        {
            MinSafetyWindowTime = TimeSpan.FromMinutes(5)
        });

        var previousState = new ThermostatRuntimeState
        {
            WasCooling = true,
            LastCoolStarted = now - TimeSpan.FromMinutes(2)
        };

        var input = CoolInput(
            now,
            temperatureFahr: 70.0,
            relativeHumidity: 40.0,
            outsideAbsoluteHumidity: 12.0,
            maxAbsHumSetPoint: 10.8);

        Assert.True(input.ControlHumidity < input.AbsoluteHumidityCoolingOnThreshold);

        var (output, _) = engine.Evaluate(
            input,
            previousState);

        Assert.True(output.Cool);
        Assert.False(output.Heat);
        Assert.True(output.Fan);
        Assert.Equal("Everything Normal", output.Reason);
    }

    [Fact]
    public void Evaluate_CoolingAlreadyRunning_TurnsOffAfterMinSafetyWindowWhenThresholdIsNotMet()
    {
        var now = TestTime();
        var engine = new ThermostatEngine(new HvacSettings
        {
            MinSafetyWindowTime = TimeSpan.FromMinutes(5)
        });

        var previousState = new ThermostatRuntimeState
        {
            WasCooling = true,
            LastCoolStarted = now - TimeSpan.FromMinutes(10)
        };

        var input = CoolInput(
            now,
            temperatureFahr: 70.0,
            relativeHumidity: 40.0,
            outsideAbsoluteHumidity: 12.0,
            maxAbsHumSetPoint: 10.8);

        Assert.True(input.ControlHumidity < input.AbsoluteHumidityCoolingOnThreshold);

        var (output, _) = engine.Evaluate(
            input,
            previousState);

        Assert.False(output.Cool);
        Assert.False(output.Heat);
        Assert.True(output.Fan);
        Assert.Equal("Everything Normal", output.Reason);
    }

    [Fact]
    public void Evaluate_CoolingRecentlyStopped_DoesNotRestartBeforeMinSafetyWindow()
    {
        var now = TestTime();
        var engine = new ThermostatEngine(new HvacSettings
        {
            MinSafetyWindowTime = TimeSpan.FromMinutes(5)
        });

        var previousState = new ThermostatRuntimeState
        {
            WasCooling = false,
            LastCoolStopped = now - TimeSpan.FromMinutes(2)
        };

        var input = CoolInput(
            now,
            temperatureFahr: 72.0,
            relativeHumidity: 60.0,
            outsideAbsoluteHumidity: 12.0,
            maxAbsHumSetPoint: 10.8);

        Assert.True(input.ControlHumidity >= input.AbsoluteHumidityCoolingOnThreshold);

        var (output, _) = engine.Evaluate(
            input,
            previousState);

        Assert.False(output.Cool);
        Assert.False(output.Heat);
        Assert.True(output.Fan);
        Assert.Equal("Everything Normal", output.Reason);
    }

    [Fact]
    public void Evaluate_CoolingRecentlyStopped_RestartsAfterMinSafetyWindowWhenThresholdIsMet()
    {
        var now = TestTime();
        var engine = new ThermostatEngine(new HvacSettings
        {
            MinSafetyWindowTime = TimeSpan.FromMinutes(5)
        });

        var previousState = new ThermostatRuntimeState
        {
            WasCooling = false,
            LastCoolStopped = now - TimeSpan.FromMinutes(6)
        };

        var input = CoolInput(
            now,
            temperatureFahr: 72.0,
            relativeHumidity: 60.0,
            outsideAbsoluteHumidity: 12.0,
            maxAbsHumSetPoint: 10.8);

        Assert.True(input.ControlHumidity >= input.AbsoluteHumidityCoolingOnThreshold);

        var (output, _) = engine.Evaluate(
            input,
            previousState);

        Assert.True(output.Cool);
        Assert.False(output.Heat);
        Assert.True(output.Fan);
        Assert.Equal("Everything Normal", output.Reason);
    }

    [Fact]
    public void Evaluate_CoolMode_Outside60OutHumidity10Point5Inside70InsideHumidity11_Cools()
    {
        var engine = new ThermostatEngine(CoolingEdgeCaseSettings());

        var input = CoolInputForAbsoluteHumidity(
            outsideTemperatureFahr: 60.0,
            outsideAbsoluteHumidity: 10.5,
            insideTemperatureFahr: 70.0,
            insideAbsoluteHumidity: 11.0,
            maxAbsHumSetPoint: 11.0);

        var (output, _) = engine.Evaluate(
            input,
            ThermostatRuntimeState.Empty);

        Assert.True(output.Cool);
        Assert.False(output.Heat);
        Assert.True(output.Fan);
        Assert.Equal("Everything Normal", output.Reason);
    }

    [Fact]
    public void Evaluate_CoolMode_Outside80OutHumidity10Point5Inside80InsideHumidity11_Cools()
    {
        var engine = new ThermostatEngine(CoolingEdgeCaseSettings());

        var input = CoolInputForAbsoluteHumidity(
            outsideTemperatureFahr: 80.0,
            outsideAbsoluteHumidity: 10.5,
            insideTemperatureFahr: 80.0,
            insideAbsoluteHumidity: 11.0,
            maxAbsHumSetPoint: 11.0);

        var (output, _) = engine.Evaluate(
            input,
            ThermostatRuntimeState.Empty);

        Assert.True(output.Cool);
        Assert.False(output.Heat);
        Assert.True(output.Fan);
        Assert.Equal("Everything Normal", output.Reason);
    }

    [Fact]
    public void Evaluate_CoolMode_Outside60OutHumidity10Point5Inside70InsideHumidity10Point9_RunsFanOnly()
    {
        var engine = new ThermostatEngine(CoolingEdgeCaseSettings());

        var input = CoolInputForAbsoluteHumidity(
            outsideTemperatureFahr: 60.0,
            outsideAbsoluteHumidity: 10.5,
            insideTemperatureFahr: 70.0,
            insideAbsoluteHumidity: 10.9,
            maxAbsHumSetPoint: 11.0);

        var (output, _) = engine.Evaluate(
            input,
            ThermostatRuntimeState.Empty);

        Assert.False(output.Cool);
        Assert.False(output.Heat);
        Assert.True(output.Fan);
        Assert.Equal("Everything Normal", output.Reason);
    }

    [Fact]
    public void Evaluate_CoolMode_Outside60OutHumidity10Point4Inside70InsideHumidity10Point9_RunsFanOnly()
    {
        var engine = new ThermostatEngine(CoolingEdgeCaseSettings());

        var input = CoolInputForAbsoluteHumidity(
            outsideTemperatureFahr: 60.0,
            outsideAbsoluteHumidity: 10.4,
            insideTemperatureFahr: 70.0,
            insideAbsoluteHumidity: 10.9,
            maxAbsHumSetPoint: 11.0);

        var (output, _) = engine.Evaluate(
            input,
            ThermostatRuntimeState.Empty);

        Assert.False(output.Cool);
        Assert.False(output.Heat);
        Assert.True(output.Fan);
        Assert.Equal("Everything Normal", output.Reason);
    }

    [Fact]
    public void Evaluate_CoolMode_Outside70OutHumidity10Point5Inside80InsideHumidity10Point9_RunsFanOnly()
    {
        var engine = new ThermostatEngine(CoolingEdgeCaseSettings());

        var input = CoolInputForAbsoluteHumidity(
            outsideTemperatureFahr: 70.0,
            outsideAbsoluteHumidity: 10.5,
            insideTemperatureFahr: 80.0,
            insideAbsoluteHumidity: 10.9,
            maxAbsHumSetPoint: 11.0);

        var (output, _) = engine.Evaluate(
            input,
            ThermostatRuntimeState.Empty);

        Assert.True(output.Cool);
        Assert.False(output.Heat);
        Assert.True(output.Fan);
        Assert.Equal("Everything Normal", output.Reason);
    }

    [Fact]
    public void Evaluate_CoolMode_Outside75OutHumidity10Point4Inside70InsideHumidity11_Cools()
    {
        var engine = new ThermostatEngine(CoolingEdgeCaseSettings());

        var input = CoolInputForAbsoluteHumidity(
            outsideTemperatureFahr: 75.0,
            outsideAbsoluteHumidity: 10.4,
            insideTemperatureFahr: 70.0,
            insideAbsoluteHumidity: 11.0,
            maxAbsHumSetPoint: 11.0);

        var (output, _) = engine.Evaluate(
            input,
            ThermostatRuntimeState.Empty);

        Assert.True(output.Cool);
        Assert.False(output.Heat);
        Assert.True(output.Fan);
        Assert.Equal("Everything Normal", output.Reason);
    }

    private static HvacSettings CoolingEdgeCaseSettings()
    {
        return new HvacSettings
        {
            MinSafetyWindowTime = TimeSpan.FromMinutes(5)
        };
    }

    private static ThermostatInput CoolInput(
        DateTimeOffset now,
        double temperatureFahr = 72.0,
        double relativeHumidity = 60.0,
        double? outsideAbsoluteHumidity = 14.0,
        double maxAbsHumSetPoint = 10.8)
    {
        return new ThermostatInput
        {
            CurrentTempFahrUp = temperatureFahr,
            RelHumidityUpstairs = relativeHumidity,
            Mode = HvacMode.Cool,
            OutsideAbsoluteHumidity = outsideAbsoluteHumidity,
            AbsoluteHumidityCoolingOnThreshold = maxAbsHumSetPoint,
            DayHeatSetPoint = 70.5,
            NightHeatSetPoint = 65.0,
            Now = now,
            LastSensorUpdate = now
        };
    }

    private static ThermostatInput CoolInputForAbsoluteHumidity(
        double outsideTemperatureFahr,
        double outsideAbsoluteHumidity,
        double insideTemperatureFahr,
        double insideAbsoluteHumidity,
        double maxAbsHumSetPoint)
    {
        var now = TestTime();

        return new ThermostatInput
        {
            CurrentTempFahrUp = insideTemperatureFahr,
            RelHumidityUpstairs = RelativeHumidityForAbsoluteHumidity(
                insideTemperatureFahr,
                insideAbsoluteHumidity),
            Mode = HvacMode.Cool,
            OutsideAbsoluteHumidity = outsideAbsoluteHumidity,
            AbsoluteHumidityCoolingOnThreshold = maxAbsHumSetPoint,
            DayHeatSetPoint = 70.5,
            NightHeatSetPoint = 65.0,
            Now = now,
            LastSensorUpdate = now
        };
    }

    private static double RelativeHumidityForAbsoluteHumidity(
        double temperatureFahr,
        double absoluteHumidity)
    {
        var temperatureC = (temperatureFahr - 32.0) * 5.0 / 9.0;

        var saturationVaporPressure =
            6.112 * Math.Exp((17.67 * temperatureC) / (temperatureC + 243.5));

        return absoluteHumidity *
               (273.15 + temperatureC) /
               (saturationVaporPressure * 2.1674);
    }

    private static DateTimeOffset TestTime()
    {
        return new DateTimeOffset(
            2026, 6, 16, 14, 0, 0,
            TimeSpan.FromHours(-4));
    }

    [Theory]
    [InlineData(72.0, 9.0)]
    [InlineData(72.1, 9.0)]
    [InlineData(71.0, 10.0)]
    [InlineData(71.9, 10.0)]
    [InlineData(70.9, 11.0)]
    [InlineData(68.0, 11.0)]
    public void Evaluate_SetsCoolingHumidityTargetBasedOnUpstairsTemperature(
        double upstairsTemperature,
        double expectedTarget)
    {
        var now = TestTime();

        var engine = new ThermostatEngine(new HvacSettings
        {
            AbsHumidityTarget72 = 9.0,
            AbsHumidityTarget71 = 10.0,
            AbsHumidityTarget70 = 11.0
        });

        var input = new ThermostatInput
        {
            CurrentTempFahrUp = upstairsTemperature,
            RelHumidityUpstairs = 60.0,
            Mode = HvacMode.Cool,
            OutsideAbsoluteHumidity = 12.0,
            DayHeatSetPoint = 70.5,
            NightHeatSetPoint = 65.0,
            Now = now,
            LastSensorUpdate = now
        };

        var (output, _) = engine.Evaluate(
            input,
            ThermostatRuntimeState.Empty);

        Assert.Equal(expectedTarget, output.MaxAbsHumSetPoint);
    }

}
using HvacController.Models;
using HvacController.Services;

namespace HvacController.Tests;

public sealed class ThermostatEngineCoolingHumidityTests
{

    [Fact]
    public void Evaluate_CoolsWhenIdealHumidityTargetIsMetAt72OrAbove()
    {
        var now = NormalCoolingTime();
        var engine = new ThermostatEngine(new HvacSettings());

        var input = CoolInputForAbsoluteHumidity(
            now,
            outsideAbsoluteHumidity: 12.0,
            insideTemperatureFahr: 72.0,
            insideAbsoluteHumidity: 9.1);

        var (output, _) = engine.Evaluate(
            input,
            ThermostatRuntimeState.Empty);

        Assert.True(output.Cool);
        Assert.False(output.Heat);
        Assert.True(output.Fan);
        Assert.Equal("Everything Normal", output.Reason);
        Assert.Equal(9.0, output.MaxAbsHumSetPoint);
    }

    [Fact]
    public void Evaluate_CoolsWhenGoodHumidityTargetIsMetAt71()
    {
        var now = NormalCoolingTime();
        var engine = new ThermostatEngine(new HvacSettings());

        var input = CoolInputForAbsoluteHumidity(
            now,
            outsideAbsoluteHumidity: 12.0,
            insideTemperatureFahr: 71.0,
            insideAbsoluteHumidity: 10.1);

        var (output, _) = engine.Evaluate(
            input,
            ThermostatRuntimeState.Empty);

        Assert.True(output.Cool);
        Assert.False(output.Heat);
        Assert.True(output.Fan);
        Assert.Equal("Everything Normal", output.Reason);
        Assert.Equal(10.0, output.MaxAbsHumSetPoint);
    }

    [Fact]
    public void Evaluate_CoolsWhenFairHumidityTargetIsMetBelow71()
    {
        var now = NormalCoolingTime();
        var engine = new ThermostatEngine(new HvacSettings());

        var input = CoolInputForAbsoluteHumidity(
            now,
            outsideAbsoluteHumidity: 12.0,
            insideTemperatureFahr: 70.9,
            insideAbsoluteHumidity: 11.1);

        var (output, _) = engine.Evaluate(
            input,
            ThermostatRuntimeState.Empty);

        Assert.True(output.Cool);
        Assert.False(output.Heat);
        Assert.True(output.Fan);
        Assert.Equal("Everything Normal", output.Reason);
        Assert.Equal(11.0, output.MaxAbsHumSetPoint);
    }

    [Fact]
    public void Evaluate_DoesNotCoolWhenCurrentHumidityIsBelowSelectedTarget()
    {
        var now = NormalCoolingTime();
        var engine = new ThermostatEngine(new HvacSettings());

        var input = CoolInputForAbsoluteHumidity(
            now,
            outsideAbsoluteHumidity: 12.0,
            insideTemperatureFahr: 71.0,
            insideAbsoluteHumidity: 9.9);

        Assert.True(input.ControlHumidity < input.HumidityTargetGood);

        var (output, _) = engine.Evaluate(
            input,
            ThermostatRuntimeState.Empty);

        Assert.False(output.Cool);
        Assert.False(output.Heat);
        Assert.True(output.Fan);
        Assert.Equal("Everything Normal", output.Reason);
        Assert.Equal(10.0, output.MaxAbsHumSetPoint);
    }

    [Fact]
    public void Evaluate_UsesFairHumidityTargetDuringHighDemandPricingWindow()
    {
        var now = HighDemandPricingTime();
        var engine = new ThermostatEngine(new HvacSettings());

        var input = CoolInputForAbsoluteHumidity(
            now,
            outsideAbsoluteHumidity: 12.0,
            insideTemperatureFahr: 72.0,
            insideAbsoluteHumidity: 10.9);

        var (output, _) = engine.Evaluate(
            input,
            ThermostatRuntimeState.Empty);

        Assert.False(output.Cool);
        Assert.False(output.Heat);
        Assert.True(output.Fan);
        Assert.Equal("Everything Normal", output.Reason);
        Assert.Equal(11.0, output.MaxAbsHumSetPoint);
    }

    [Fact]
    public void Evaluate_CoolsDuringHighDemandPricingWindowWhenFairTargetIsMet()
    {
        var now = HighDemandPricingTime();
        var engine = new ThermostatEngine(new HvacSettings());

        var input = CoolInputForAbsoluteHumidity(
            now,
            outsideAbsoluteHumidity: 12.0,
            insideTemperatureFahr: 72.0,
            insideAbsoluteHumidity: 11.1);

        var (output, _) = engine.Evaluate(
            input,
            ThermostatRuntimeState.Empty);

        Assert.True(output.Cool);
        Assert.False(output.Heat);
        Assert.True(output.Fan);
        Assert.Equal("Everything Normal", output.Reason);
        Assert.Equal(11.0, output.MaxAbsHumSetPoint);
    }

    [Fact]
    public void Evaluate_ReturnsSafeOffWhenControlHumidityIsMissingEvenIfCoolingWasAlreadyRunning()
    {
        var now = NormalCoolingTime();
        var engine = new ThermostatEngine(new HvacSettings());

        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 72.0,
            RelHumidityUpstairs = null,
            Mode = HvacMode.Cool,
            OutsideAbsoluteHumidity = 12.0,
            DayHeatSetPoint = 70.5,
            NightHeatSetPoint = 65.0,
            HumidityTargetIdeal = 9.0,
            HumidityTargetGood = 10.0,
            HumidityTargetFair = 11.0,
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
        var now = NormalCoolingTime();
        var engine = new ThermostatEngine(new HvacSettings
        {
            MinSafetyWindowTime = TimeSpan.FromMinutes(5)
        });

        var previousState = new ThermostatRuntimeState
        {
            WasCooling = true,
            LastCoolStarted = now - TimeSpan.FromMinutes(2)
        };

        var input = CoolInputForAbsoluteHumidity(
            now,
            outsideAbsoluteHumidity: 12.0,
            insideTemperatureFahr: 72.0,
            insideAbsoluteHumidity: 8.0);

        var (output, _) = engine.Evaluate(
            input,
            previousState);

        Assert.True(output.Cool);
        Assert.False(output.Heat);
        Assert.True(output.Fan);
    }

    [Fact]
    public void Evaluate_CoolingAlreadyRunning_TurnsOffAfterMinSafetyWindowWhenTargetIsNotMet()
    {
        var now = NormalCoolingTime();
        var engine = new ThermostatEngine(new HvacSettings
        {
            MinSafetyWindowTime = TimeSpan.FromMinutes(5)
        });

        var previousState = new ThermostatRuntimeState
        {
            WasCooling = true,
            LastCoolStarted = now - TimeSpan.FromMinutes(10)
        };

        var input = CoolInputForAbsoluteHumidity(
            now,
            outsideAbsoluteHumidity: 12.0,
            insideTemperatureFahr: 72.0,
            insideAbsoluteHumidity: 8.0);

        var (output, _) = engine.Evaluate(
            input,
            previousState);

        Assert.False(output.Cool);
        Assert.False(output.Heat);
        Assert.True(output.Fan);
    }

    [Fact]
    public void Evaluate_CoolingRecentlyStopped_DoesNotRestartBeforeMinSafetyWindow()
    {
        var now = NormalCoolingTime();
        var engine = new ThermostatEngine(new HvacSettings
        {
            MinSafetyWindowTime = TimeSpan.FromMinutes(5)
        });

        var previousState = new ThermostatRuntimeState
        {
            WasCooling = false,
            LastCoolStopped = now - TimeSpan.FromMinutes(2)
        };

        var input = CoolInputForAbsoluteHumidity(
            now,
            outsideAbsoluteHumidity: 12.0,
            insideTemperatureFahr: 72.0,
            insideAbsoluteHumidity: 9.5);

        var (output, _) = engine.Evaluate(
            input,
            previousState);

        Assert.False(output.Cool);
        Assert.False(output.Heat);
        Assert.True(output.Fan);
    }

    [Theory]
    [InlineData(72.0, 9.0)]
    [InlineData(72.1, 9.0)]
    [InlineData(71.0, 10.0)]
    [InlineData(71.9, 10.0)]
    [InlineData(70.9, 11.0)]
    [InlineData(68.0, 11.0)]
    public void Evaluate_SetsCoolingHumidityTargetBasedOnTemperatureOutsideHighDemandWindow(
        double upstairsTemperature,
        double expectedTarget)
    {
        var now = NormalCoolingTime();
        var engine = new ThermostatEngine(new HvacSettings());

        var input = CoolInput(
            now,
            temperatureFahr: upstairsTemperature,
            relativeHumidity: 50.0,
            outsideAbsoluteHumidity: 12.0);

        var (output, _) = engine.Evaluate(
            input,
            ThermostatRuntimeState.Empty);

        Assert.Equal(expectedTarget, output.MaxAbsHumSetPoint);
    }

    [Theory]
    [InlineData("2026-06-16T13:59:59-04:00", false)]
    [InlineData("2026-06-16T14:00:00-04:00", true)]
    [InlineData("2026-06-16T18:59:59-04:00", true)]
    [InlineData("2026-06-16T19:00:00-04:00", false)]
    [InlineData("2026-10-01T15:00:00-04:00", false)]
    [InlineData("2026-07-04T15:00:00-04:00", false)]
    public void IsHighDemandPricingWindow_ReturnsExpectedValue(
        string localTime,
        bool expected)
    {
        var currentLocalTime = DateTimeOffset.Parse(localTime).DateTime;

        Assert.Equal(
            expected,
            ThermostatEngine.IsHighDemandPricingWindow(currentLocalTime));
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
            RelHumidityUpstairs = relativeHumidity,
            Mode = HvacMode.Cool,
            OutsideAbsoluteHumidity = outsideAbsoluteHumidity,
            DayHeatSetPoint = 70.5,
            NightHeatSetPoint = 65.0,
            HumidityTargetIdeal = 9.0,
            HumidityTargetGood = 10.0,
            HumidityTargetFair = 11.0,
            Now = now,
            LastSensorUpdate = now
        };
    }

    private static ThermostatInput CoolInputForAbsoluteHumidity(
        DateTimeOffset now,
        double outsideAbsoluteHumidity,
        double insideTemperatureFahr,
        double insideAbsoluteHumidity)
    {
        return new ThermostatInput
        {
            CurrentTempFahrUp = insideTemperatureFahr,
            RelHumidityUpstairs = RelativeHumidityForAbsoluteHumidity(
                insideTemperatureFahr,
                insideAbsoluteHumidity),
            Mode = HvacMode.Cool,
            OutsideAbsoluteHumidity = outsideAbsoluteHumidity,
            DayHeatSetPoint = 70.5,
            NightHeatSetPoint = 65.0,
            HumidityTargetIdeal = 9.0,
            HumidityTargetGood = 10.0,
            HumidityTargetFair = 11.0,
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

    private static DateTimeOffset NormalCoolingTime()
    {
        return new DateTimeOffset(
            2026, 6, 16, 13, 0, 0,
            TimeSpan.FromHours(-4));
    }

    private static DateTimeOffset HighDemandPricingTime()
    {
        return new DateTimeOffset(
            2026, 6, 16, 14, 0, 0,
            TimeSpan.FromHours(-4));
    }
}
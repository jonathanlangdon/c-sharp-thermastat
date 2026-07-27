using HvacController.Models;
using HvacController.Services;

namespace HvacController.Tests;

public sealed class ThermostatEngineTests
{
    [Fact]
    public void CoolMode_WhenAbsoluteHumidityIsBelowThreshold_DoesNotTurnOnCooling()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings());

        var input = CoolInput(
            now,
            temperatureFahr: 68,
            relativeHumidity: 50,
            outsideAbsoluteHumidity: 15.0,
            humidityTargetIdeal: 9);

        var (output, _) = engine.Evaluate(input, ThermostatRuntimeState.Empty);

        Assert.False(output.Heat);
        Assert.False(output.Cool);
        Assert.True(output.Fan);
    }

    [Fact]
    public void CoolMode_WhenAbsoluteHumidityIsAtOrAboveThreshold_TurnsOnCoolingAndFan()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings());

        var input = CoolInput(
            now,
            temperatureFahr: 72,
            relativeHumidity: 50,
            outsideAbsoluteHumidity: 15.0,
            humidityTargetIdeal: 9);

        var (output, _) = engine.Evaluate(input, ThermostatRuntimeState.Empty);

        Assert.False(output.Heat);
        Assert.True(output.Cool);
        Assert.True(output.Fan);
    }

    [Fact]
    public void Cooling_WhenAlreadyRunning_ContinuesWhenHumidityIsStillAtOrAboveThreshold()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            MinSafetyWindowTime = TimeSpan.FromMinutes(5)
        });

        var previousState = ThermostatRuntimeState.Empty with
        {
            WasCooling = true,
            LastCoolStarted = now.AddMinutes(-10)
        };

        var input = CoolInput(
            now,
            temperatureFahr: 72,
            relativeHumidity: 56,
            outsideAbsoluteHumidity: 15.0,
            humidityTargetIdeal: 9);

        var (output, _) = engine.Evaluate(input, previousState);

        Assert.False(output.Heat);
        Assert.True(output.Cool);
        Assert.True(output.Fan);
    }

    [Fact]
    public void Cooling_WhenAlreadyRunningAndHumidityFallsBelowThreshold_TurnsCoolingOff()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            MinSafetyWindowTime = TimeSpan.FromMinutes(5)
        });

        var previousState = ThermostatRuntimeState.Empty with
        {
            WasCooling = true,
            LastCoolStarted = now.AddMinutes(-10)
        };

        var input = CoolInput(
            now,
            temperatureFahr: 72,
            relativeHumidity: 44,
            outsideAbsoluteHumidity: 15.0,
            humidityTargetIdeal: 9);

        var (output, _) = engine.Evaluate(input, previousState);

        Assert.False(output.Heat);
        Assert.False(output.Cool);
        Assert.True(output.Fan);
    }

    [Fact]
    public void HeatMode_WhenTemperatureIsBelowSetpoint_TurnsOnHeatOnly()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings());

        var input = HeatInput(
            now,
            temperatureFahr: 68,
            relativeHumidity: 48,
            dayHeatSetPoint: 70.5,
            nightHeatSetPoint: 65.0) with
        {
            LastMotionDetected = now.AddMinutes(-30)
        };

        var (output, _) = engine.Evaluate(input, ThermostatRuntimeState.Empty);

        Assert.True(output.Heat);
        Assert.False(output.Cool);
        Assert.True(output.Fan);
    }

    [Fact]
    public void SensorTimeout_TurnsEverythingOff()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            SensorTimeout = TimeSpan.FromMinutes(3)
        });

        var input = CoolInput(
            now,
            temperatureFahr: 80,
            relativeHumidity: 48,
            outsideAbsoluteHumidity: 15.0) with
        {
            LastSensorUpdate = now.AddMinutes(-4)
        };

        var (output, _) = engine.Evaluate(input, ThermostatRuntimeState.Empty);

        Assert.False(output.Heat);
        Assert.False(output.Cool);
        Assert.False(output.Fan);
        Assert.Equal("Sensor timeout", output.Reason);
    }

    [Fact]
    public void Cooling_WhenRecentlyStopped_DoesNotRestartBeforeMinSafetyWindowTime()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            MinSafetyWindowTime = TimeSpan.FromMinutes(5)
        });

        var previousState = ThermostatRuntimeState.Empty with
        {
            WasCooling = false,
            LastCoolStopped = now.AddMinutes(-2)
        };

        var input = CoolInput(
            now,
            temperatureFahr: 80,
            relativeHumidity: 48,
            outsideAbsoluteHumidity: 15.0,
            humidityTargetIdeal: 9);

        var (output, _) = engine.Evaluate(input, previousState);

        Assert.False(output.Cool);
        Assert.False(output.Heat);
        Assert.True(output.Fan);
    }

    [Fact]
    public void Cooling_WhenAlreadyRunning_StaysOnUntilMinSafetyWindowTimeSatisfied()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            MinSafetyWindowTime = TimeSpan.FromMinutes(5)
        });

        var previousState = ThermostatRuntimeState.Empty with
        {
            WasCooling = true,
            LastCoolStarted = now.AddMinutes(-2)
        };

        var input = CoolInput(
            now,
            temperatureFahr: 71,
            relativeHumidity: 48,
            outsideAbsoluteHumidity: 15.0,
            humidityTargetIdeal: 9);

        var (output, _) = engine.Evaluate(input, previousState);

        Assert.True(output.Cool);
        Assert.True(output.Fan);
        Assert.False(output.Heat);
    }

    [Fact]
    public void Idle_WhenCoolModeAndNoCoolingNeeded_TurnsFanOn()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings());

        var input = CoolInput(
            now,
            temperatureFahr: 72,
            relativeHumidity: 40,
            outsideAbsoluteHumidity: 15.0);

        var (output, _) = engine.Evaluate(input, ThermostatRuntimeState.Empty);

        Assert.False(output.Heat);
        Assert.False(output.Cool);
        Assert.True(output.Fan);
    }

    [Fact]
    public void Idle_WhenHeatModeAndNoHeatingNeeded_TurnsFanOn()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings());

        var input = HeatInput(
            now,
            temperatureFahr: 70,
            relativeHumidity: 48,
            dayHeatSetPoint: 70.5,
            nightHeatSetPoint: 65.0);

        var (output, _) = engine.Evaluate(input, ThermostatRuntimeState.Empty);

        Assert.False(output.Heat);
        Assert.False(output.Cool);
        Assert.True(output.Fan);
    }

    [Fact]
    public void HeatMode_WhenAlreadyRunning_StaysOnUntilMinSafetyWindowTimeSatisfied()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            MinSafetyWindowTime = TimeSpan.FromMinutes(5)
        });

        var previousState = ThermostatRuntimeState.Empty with
        {
            WasHeating = true,
            LastHeatStarted = now.AddMinutes(-2)
        };

        var input = HeatInput(
            now,
            temperatureFahr: 71,
            relativeHumidity: 48,
            dayHeatSetPoint: 70.5,
            nightHeatSetPoint: 65.0);

        var (output, _) = engine.Evaluate(input, previousState);

        Assert.True(output.Heat);
        Assert.False(output.Cool);
        Assert.True(output.Fan);
    }

    [Fact]
    public void Heat_WhenRecentlyStopped_DoesNotRestartBeforeMinSafetyWindowTime()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            MinSafetyWindowTime = TimeSpan.FromMinutes(5)
        });

        var previousState = ThermostatRuntimeState.Empty with
        {
            WasHeating = false,
            LastHeatStopped = now.AddMinutes(-2)
        };

        var input = HeatInput(
            now,
            temperatureFahr: 60,
            relativeHumidity: 48,
            dayHeatSetPoint: 70.5,
            nightHeatSetPoint: 65.0);

        var (output, _) = engine.Evaluate(input, previousState);

        Assert.False(output.Heat);
        Assert.False(output.Cool);
        Assert.True(output.Fan);
    }

    [Fact]
    public void HeatMode_UsesDayHeatSetPointFromInput()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings());

        var input = HeatInput(
            now,
            temperatureFahr: 68,
            relativeHumidity: 40,
            dayHeatSetPoint: 70.0,
            nightHeatSetPoint: 65.0) with
        {
            LastMotionDetected = now.AddMinutes(-30)
        };

        var (output, _) = engine.Evaluate(input, ThermostatRuntimeState.Empty);

        Assert.True(output.Heat);
        Assert.False(output.Cool);
        Assert.True(output.Fan);
        Assert.Equal(70.0, output.HeatSetPointFahr);
    }

    [Fact]
    public void HeatMode_AtNight_UsesNightHeatSetPoint()
    {
        var now = DateTimeOffset.Parse("2026-06-12T23:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings());

        var input = HeatInput(
            now,
            temperatureFahr: 64,
            relativeHumidity: 40,
            dayHeatSetPoint: 70.0,
            nightHeatSetPoint: 65.0);

        var (output, _) = engine.Evaluate(input, ThermostatRuntimeState.Empty);

        Assert.True(output.Heat);
        Assert.False(output.Cool);
        Assert.True(output.Fan);
        Assert.Equal(65.0, output.HeatSetPointFahr);
    }

    [Fact]
    public void HeatMode_AtNight_DoesNotHeatUntilBelowNightSetPointMinusDifferential()
    {
        var now = DateTimeOffset.Parse("2026-06-12T23:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            TemperatureDifferentialF = 1.0
        });

        var input = HeatInput(
            now,
            temperatureFahr: 64.5,
            relativeHumidity: 40,
            dayHeatSetPoint: 70.0,
            nightHeatSetPoint: 65.0);

        var (output, _) = engine.Evaluate(input, ThermostatRuntimeState.Empty);

        Assert.False(output.Heat);
        Assert.False(output.Cool);
        Assert.True(output.Fan);
        Assert.Equal(65.0, output.HeatSetPointFahr);
    }

    [Fact]
    public void HeatMode_DuringDay_UsesDayHeatSetPoint()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings());

        var input = HeatInput(
            now,
            temperatureFahr: 68,
            relativeHumidity: 40,
            dayHeatSetPoint: 70.0,
            nightHeatSetPoint: 65.0) with
        {
            LastMotionDetected = now.AddMinutes(-30)
        };

        var (output, _) = engine.Evaluate(input, ThermostatRuntimeState.Empty);

        Assert.True(output.Heat);
        Assert.False(output.Cool);
        Assert.True(output.Fan);
        Assert.Equal(70.0, output.HeatSetPointFahr);
    }

    [Fact]
    public void HeatMode_DuringDayWithRecentMotion_UsesDayHeatSetPoint()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            MotionSetPointHoldTime = TimeSpan.FromHours(2)
        });

        var input = HeatInput(
            now,
            temperatureFahr: 68,
            relativeHumidity: 40,
            dayHeatSetPoint: 70.0,
            nightHeatSetPoint: 65.0) with
        {
            LastMotionDetected = now.AddHours(-1)
        };

        var (output, _) = engine.Evaluate(input, ThermostatRuntimeState.Empty);

        Assert.True(output.Heat);
        Assert.False(output.Cool);
        Assert.True(output.Fan);
        Assert.Equal(70.0, output.HeatSetPointFahr);
    }

    [Fact]
    public void HeatMode_DuringDayWithoutRecentMotion_UsesNightHeatSetPoint()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            MotionSetPointHoldTime = TimeSpan.FromHours(2)
        });

        var input = HeatInput(
            now,
            temperatureFahr: 68,
            relativeHumidity: 40,
            dayHeatSetPoint: 70.0,
            nightHeatSetPoint: 65.0) with
        {
            LastMotionDetected = now.AddHours(-3)
        };

        var (output, _) = engine.Evaluate(input, ThermostatRuntimeState.Empty);

        Assert.False(output.Heat);
        Assert.False(output.Cool);
        Assert.True(output.Fan);
        Assert.Equal(65.0, output.HeatSetPointFahr);
    }

    [Fact]
    public void NoTemperatureReading_TurnsEverythingOff()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings());

        var input = HeatInput(
            now,
            temperatureFahr: 70,
            relativeHumidity: 40,
            dayHeatSetPoint: 70.5,
            nightHeatSetPoint: 65.0) with
        {
            CurrentTempFahrUp = null,
            LastMotionDetected = now
        };

        var (output, _) = engine.Evaluate(input, ThermostatRuntimeState.Empty);

        Assert.False(output.Heat);
        Assert.False(output.Cool);
        Assert.False(output.Fan);
        Assert.Equal("No temperature reading", output.Reason);
    }

    [Fact]
    public void NoHumidityReading_TurnsEverythingOff()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings());

        var input = HeatInput(
            now,
            temperatureFahr: 70,
            relativeHumidity: 40,
            dayHeatSetPoint: 70.5,
            nightHeatSetPoint: 65.0) with
        {
            RelHumidityUpstairs = null,
            LastMotionDetected = now
        };

        var (output, _) = engine.Evaluate(input, ThermostatRuntimeState.Empty);

        Assert.False(output.Heat);
        Assert.False(output.Cool);
        Assert.False(output.Fan);
        Assert.Equal("No humidity reading", output.Reason);
    }

    [Fact]
    public void CoolMode_WhenUpstairsHumidityIsAboveThreshold_TurnsOnCooling()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings());

        var input = CoolInput(
            now,
            temperatureFahr: 72,
            relativeHumidity: 50,
            outsideAbsoluteHumidity: 15.0,
            humidityTargetIdeal: 9.5,
            humidityTargetGood: 9.5,
            humidityTargetFair: 9.5) with
        {
            CurrentTempFahrDown = 65,
            RelHumidityDownstairs = 40,
            LastMotionDetected = now
        };

        var (output, _) = engine.Evaluate(input, ThermostatRuntimeState.Empty);

        Assert.False(output.Heat);
        Assert.True(output.Cool);
        Assert.True(output.Fan);
    }

    [Fact]
    public void CoolMode_WhenDownstairsHumidityIsAboveThreshold_TurnsOnCooling()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings());

        var input = CoolInput(
            now,
            temperatureFahr: 68,
            relativeHumidity: 40,
            outsideAbsoluteHumidity: 15.0,
            humidityTargetIdeal: 9,
            humidityTargetGood: 10,
            humidityTargetFair: 11) with
        {
            CurrentTempFahrDown = 72,
            RelHumidityDownstairs = 55,
            LastMotionDetected = now
        };

        var (output, _) = engine.Evaluate(input, ThermostatRuntimeState.Empty);

        Assert.False(output.Heat);
        Assert.False(output.Cool);
        Assert.True(output.Fan);
    }

    [Fact]
    public void CoolMode_WhenBothHumidityReadingsAreBelowThreshold_DoesNotTurnOnCooling()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings());

        var input = CoolInput(
            now,
            temperatureFahr: 68,
            relativeHumidity: 40,
            outsideAbsoluteHumidity: 15.0,
            humidityTargetIdeal: 9.5,
            humidityTargetGood: 9.5,
            humidityTargetFair: 9.5) with
        {
            CurrentTempFahrDown = 65,
            RelHumidityDownstairs = 40,
            LastMotionDetected = now
        };

        var (output, _) = engine.Evaluate(input, ThermostatRuntimeState.Empty);

        Assert.False(output.Heat);
        Assert.False(output.Cool);
        Assert.True(output.Fan);
    }

    [Theory]
    [InlineData("2026-06-01T13:59:59", false)]
    [InlineData("2026-06-01T14:00:00", true)]
    [InlineData("2026-06-01T18:59:59", true)]
    [InlineData("2026-06-01T19:00:00", false)]
    [InlineData("2026-05-31T15:00:00", false)]
    [InlineData("2026-06-01T15:00:00", true)]
    [InlineData("2026-09-30T15:00:00", true)]
    [InlineData("2026-10-01T15:00:00", false)]
    [InlineData("2026-07-11T15:00:00", false)] // Saturday
    [InlineData("2026-07-12T15:00:00", false)] // Sunday
    public void IsHighDemandPricingWindow_ReturnsExpectedResult(
        string currentLocalTimeValue,
        bool expected)
    {
        var currentLocalTime = DateTime.Parse(currentLocalTimeValue);

        var result = ThermostatEngine.IsHighDemandPricingWindow(currentLocalTime);

        Assert.Equal(expected, result);
    }

    private static ThermostatInput CoolInput(
        DateTimeOffset now,
        double temperatureFahr,
        double relativeHumidity,
        double? outsideAbsoluteHumidity,
        double humidityTargetIdeal = 9.0,
        double humidityTargetGood = 10.0,
        double humidityTargetFair = 11.0)
    {
        return new ThermostatInput
        {
            CurrentTempFahrUp = temperatureFahr,
            RelHumidityUpstairs = relativeHumidity,
            Mode = HvacMode.Cool,
            OutsideAbsoluteHumidity = outsideAbsoluteHumidity,

            DayHeatSetPoint = 70.5,
            NightHeatSetPoint = 65.0,

            HumidityTargetIdeal = humidityTargetIdeal,
            HumidityTargetGood = humidityTargetGood,
            HumidityTargetFair = humidityTargetFair,

            Now = now,
            LastSensorUpdate = now
        };
    }

    private static ThermostatInput HeatInput(
        DateTimeOffset now,
        double temperatureFahr,
        double relativeHumidity,
        double dayHeatSetPoint,
        double nightHeatSetPoint)
    {
        return new ThermostatInput
        {
            CurrentTempFahrUp = temperatureFahr,
            RelHumidityUpstairs = relativeHumidity,
            Mode = HvacMode.Heat,

            DayHeatSetPoint = dayHeatSetPoint,
            NightHeatSetPoint = nightHeatSetPoint,

            HumidityTargetIdeal = 9.0,
            HumidityTargetGood = 10.0,
            HumidityTargetFair = 11.0,

            Now = now,
            LastSensorUpdate = now
        };
    }

    private static DateTimeOffset NormalCoolingTime()
    {
        return new DateTimeOffset(
            2026, 6, 12, 13, 0, 0,
            TimeSpan.FromHours(-4));
    }

}
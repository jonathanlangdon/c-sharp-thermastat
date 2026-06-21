using HvacController.Models;
using HvacController.Services;

namespace HvacController.Tests;

public sealed class ThermostatEngineTests
{
    [Fact]
    public void CoolMode_WhenAbsoluteHumidityIsBelowThreshold_DoesNotTurnOnCooling()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            AbsoluteHumidityCoolingOnThreshold = 9.5
        });

        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 68,
            HumidityUpstairs = 50,
            Mode = HvacMode.Cool,
            Now = now,
            LastSensorUpdate = now
        };

        var (output, _) = engine.Evaluate(input, ThermostatRuntimeState.Empty);

        Assert.False(output.Heat);
        Assert.False(output.Cool);
        Assert.True(output.Fan);
    }

    [Fact]
    public void CoolMode_WhenAbsoluteHumidityIsAtOrAboveThreshold_TurnsOnCoolingAndFan()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            AbsoluteHumidityCoolingOnThreshold = 9.5
        });

        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 72,
            HumidityUpstairs = 50,
            OutsideAbsoluteHumidity = 15.0,
            Mode = HvacMode.Cool,
            Now = now,
            LastSensorUpdate = now
        };

        var (output, _) = engine.Evaluate(input, ThermostatRuntimeState.Empty);

        Assert.False(output.Heat);
        Assert.True(output.Cool);
        Assert.True(output.Fan);
    }

    [Fact]
    public void Cooling_WhenAlreadyRunning_ContinuesUntilHumidityFallsBelowOffThreshold()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            AbsoluteHumidityCoolingOnThreshold = 9.5,
            MinimumRunTime = TimeSpan.FromMinutes(5)
        });

        var previousState = ThermostatRuntimeState.Empty with
        {
            WasCooling = true,
            LastCoolStarted = now.AddMinutes(-10)
        };

        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 72,
            HumidityUpstairs = 56,
            Mode = HvacMode.Cool,
            OutsideAbsoluteHumidity = 15.0,
            Now = now,
            LastSensorUpdate = now
        };

        var (output, _) = engine.Evaluate(input, previousState);

        Assert.False(output.Heat);
        Assert.True(output.Cool);
        Assert.True(output.Fan);
    }

    [Fact]
    public void Cooling_WhenAlreadyRunningAndHumidityFallsBelowOffThreshold_TurnsCoolingOff()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            AbsoluteHumidityCoolingOnThreshold = 9.5,
            MinimumRunTime = TimeSpan.FromMinutes(5)
        });

        var previousState = ThermostatRuntimeState.Empty with
        {
            WasCooling = true,
            LastCoolStarted = now.AddMinutes(-10)
        };

        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 72,
            HumidityUpstairs = 44,
            Mode = HvacMode.Cool,
            Now = now,
            LastSensorUpdate = now
        };

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

        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 68,
            HumidityUpstairs = 48,
            LastMotionDetected = now.AddMinutes(-30),
            Mode = HvacMode.Heat,
            Now = now,
            LastSensorUpdate = now
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

        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 80,
            HumidityUpstairs = 48,
            Mode = HvacMode.Cool,
            Now = now,
            LastSensorUpdate = now.AddMinutes(-4)
        };

        var (output, _) = engine.Evaluate(input, ThermostatRuntimeState.Empty);

        Assert.False(output.Heat);
        Assert.False(output.Cool);
        Assert.False(output.Fan);
        Assert.Equal("Sensor timeout", output.Reason);
    }

    [Fact]
    public void Cooling_WhenRecentlyStopped_DoesNotRestartBeforeMinimumRunTime()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            MinimumRunTime = TimeSpan.FromMinutes(5)
        });

        var previousState = ThermostatRuntimeState.Empty with
        {
            WasCooling = false,
            LastCoolStopped = now.AddMinutes(-2)
        };

        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 80,
            HumidityUpstairs = 48,
            Mode = HvacMode.Cool,
            Now = now,
            LastSensorUpdate = now
        };

        var (output, _) = engine.Evaluate(input, previousState);

        Assert.False(output.Cool);
        Assert.False(output.Heat);
        Assert.True(output.Fan);
    }

    [Fact]
    public void Cooling_WhenAlreadyRunning_StaysOnUntilMinimumRunTimeSatisfied()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            MinimumRunTime = TimeSpan.FromMinutes(5)
        });

        var previousState = ThermostatRuntimeState.Empty with
        {
            WasCooling = true,
            LastCoolStarted = now.AddMinutes(-2)
        };

        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 71,
            HumidityUpstairs = 48,
            Mode = HvacMode.Cool,
            OutsideAbsoluteHumidity = 15.0,
            Now = now,
            LastSensorUpdate = now
        };

        var (output, _) = engine.Evaluate(input, previousState);

        Assert.True(output.Cool);
        Assert.True(output.Fan);
        Assert.False(output.Heat);
    }

    [Fact]
    public void Idle_WhenCoolModeAndNoCoolingNeeded_TurnsFanOn()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings {});

        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 72,
            HumidityUpstairs = 40,
            Mode = HvacMode.Cool,
            Now = now,
            LastSensorUpdate = now
        };

        var (output, _) = engine.Evaluate(input, ThermostatRuntimeState.Empty);

        Assert.False(output.Heat);
        Assert.False(output.Cool);
        Assert.True(output.Fan);
    }

    [Fact]
    public void Idle_WhenHeatModeAndNoHeatingNeeded_TurnsFanOn()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings {});

        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 70,
            HumidityUpstairs = 48,
            Mode = HvacMode.Heat,
            Now = now,
            LastSensorUpdate = now
        };

        var (output, _) = engine.Evaluate(input, ThermostatRuntimeState.Empty);

        Assert.False(output.Heat);
        Assert.False(output.Cool);
        Assert.True(output.Fan);
    }

    [Fact]
    public void HeatMode_WhenAlreadyRunning_StaysOnUntilMinimumRunTimeSatisfied()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            MinimumRunTime = TimeSpan.FromMinutes(5)
        });

        var previousState = ThermostatRuntimeState.Empty with
        {
            WasHeating = true,
            LastHeatStarted = now.AddMinutes(-2)
        };

        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 71,
            HumidityUpstairs = 48,
            Mode = HvacMode.Heat,
            Now = now,
            LastSensorUpdate = now
        };

        var (output, _) = engine.Evaluate(input, previousState);

        Assert.True(output.Heat);
        Assert.False(output.Cool);
        Assert.True(output.Fan);
    }

    [Fact]
    public void Heat_WhenRecentlyStopped_DoesNotRestartBeforeMinimumRunTime()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            MinimumRunTime = TimeSpan.FromMinutes(5)
        });

        var previousState = ThermostatRuntimeState.Empty with
        {
            WasHeating = false,
            LastHeatStopped = now.AddMinutes(-2)
        };

        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 60,
            HumidityUpstairs = 48,
            Mode = HvacMode.Heat,
            Now = now,
            LastSensorUpdate = now
        };

        var (output, _) = engine.Evaluate(input, previousState);

        Assert.False(output.Heat);
        Assert.False(output.Cool);
        Assert.True(output.Fan);
    }

    [Fact]
    public void HeatMode_UsesDayHeatSetPointFromSettings()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            DayHeatSetPoint = 70.0
        });

        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 68,
            HumidityUpstairs = 40,
            LastMotionDetected = now.AddMinutes(-30),
            Mode = HvacMode.Heat,
            Now = now,
            LastSensorUpdate = now
        };

        var (output, _) = engine.Evaluate(input, ThermostatRuntimeState.Empty);

        Assert.True(output.Heat);
        Assert.False(output.Cool);
        Assert.True(output.Fan);
    }

    [Fact]
    public void HeatMode_AtNight_Uses65DegreeHeatSetPoint()
    {
        var now = DateTimeOffset.Parse("2026-06-12T23:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            DayHeatSetPoint = 70.0,
            NightHeatSetPoint = 65.0
        });

        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 64,
            HumidityUpstairs = 40,
            Mode = HvacMode.Heat,
            Now = now,
            LastSensorUpdate = now
        };

        var (output, _) = engine.Evaluate(input, ThermostatRuntimeState.Empty);

        Assert.True(output.Heat);
        Assert.False(output.Cool);
        Assert.True(output.Fan);
    }

    [Fact]
    public void HeatMode_AtNight_DoesNotHeatUntilBelow65DegreeSetPointMinusDifferential()
    {
        var now = DateTimeOffset.Parse("2026-06-12T23:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            DayHeatSetPoint = 70.0,
            NightHeatSetPoint = 65.0,
            TemperatureDifferentialF = 1.0
        });

        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 64.5,
            HumidityUpstairs = 40,
            Mode = HvacMode.Heat,
            Now = now,
            LastSensorUpdate = now
        };

        var (output, _) = engine.Evaluate(input, ThermostatRuntimeState.Empty);

        Assert.False(output.Heat);
        Assert.False(output.Cool);
        Assert.True(output.Fan);
    }

    [Fact]
    public void HeatMode_DuringDay_Uses70DegreeHeatSetPoint()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            DayHeatSetPoint = 70.0,
            NightHeatSetPoint = 65.0
        });

        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 68,
            HumidityUpstairs = 40,
            LastMotionDetected = now.AddMinutes(-30),
            Mode = HvacMode.Heat,
            Now = now,
            LastSensorUpdate = now
        };

        var (output, _) = engine.Evaluate(input, ThermostatRuntimeState.Empty);

        Assert.True(output.Heat);
        Assert.False(output.Cool);
        Assert.True(output.Fan);
    }

    [Fact]
    public void HeatMode_DuringDayWithRecentMotion_UsesDayHeatSetPoint()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            DayHeatSetPoint = 70.0,
            NightHeatSetPoint = 65.0,
            MotionSetPointHoldTime = TimeSpan.FromHours(2)
        });

        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 68,
            HumidityUpstairs = 40,
            Mode = HvacMode.Heat,
            Now = now,
            LastSensorUpdate = now,
            LastMotionDetected = now.AddHours(-1)
        };

        var (output, _) = engine.Evaluate(input, ThermostatRuntimeState.Empty);

        Assert.True(output.Heat);
        Assert.False(output.Cool);
        Assert.True(output.Fan);
    }

    [Fact]
    public void HeatMode_DuringDayWithoutRecentMotion_UsesNightHeatSetPoint()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            DayHeatSetPoint = 70.0,
            NightHeatSetPoint = 65.0,
            MotionSetPointHoldTime = TimeSpan.FromHours(2)
        });

        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 68,
            HumidityUpstairs = 40,
            Mode = HvacMode.Heat,
            Now = now,
            LastSensorUpdate = now,
            LastMotionDetected = now.AddHours(-3)
        };

        var (output, _) = engine.Evaluate(input, ThermostatRuntimeState.Empty);

        Assert.False(output.Heat);
        Assert.False(output.Cool);
        Assert.True(output.Fan);
    }
    [Fact]
    public void NoTemperatureReading_TurnsEverythingOff()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings());

        var input = new ThermostatInput
        {
            CurrentTempFahrUp = null,
            HumidityUpstairs = 40,
            Mode = HvacMode.Heat,
            Now = now,
            LastSensorUpdate = now,
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

        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 70,
            HumidityUpstairs = null,
            Mode = HvacMode.Heat,
            Now = now,
            LastSensorUpdate = now,
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
        var engine = new ThermostatEngine(new HvacSettings
        {
            AbsoluteHumidityCoolingOnThreshold = 9.5
        });

        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 72,
            HumidityUpstairs = 50,      // AH about 9.83
            CurrentTempFahrDown = 65,
            HumidityDownstairs = 40,    // AH below threshold
            Mode = HvacMode.Cool,
            OutsideAbsoluteHumidity = 15.0,
            Now = now,
            LastSensorUpdate = now,
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
        var engine = new ThermostatEngine(new HvacSettings
        {
            AbsoluteHumidityCoolingOnThreshold = 9.5
        });

        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 68,
            HumidityUpstairs = 40,      // AH below threshold
            CurrentTempFahrDown = 72,
            HumidityDownstairs = 50,    // AH about 9.83
            Mode = HvacMode.Cool,
            OutsideAbsoluteHumidity = 15.0,
            Now = now,
            LastSensorUpdate = now,
            LastMotionDetected = now
        };

        var (output, _) = engine.Evaluate(input, ThermostatRuntimeState.Empty);

        Assert.False(output.Heat);
        Assert.True(output.Cool);
        Assert.True(output.Fan);
    }

    [Fact]
    public void CoolMode_WhenBothHumidityReadingsAreBelowThreshold_DoesNotTurnOnCooling()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            AbsoluteHumidityCoolingOnThreshold = 9.5
        });

        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 68,
            HumidityUpstairs = 40,      // AH below threshold
            CurrentTempFahrDown = 65,
            HumidityDownstairs = 40,    // AH below threshold
            Mode = HvacMode.Cool,
            Now = now,
            LastSensorUpdate = now,
            LastMotionDetected = now
        };

        var (output, _) = engine.Evaluate(input, ThermostatRuntimeState.Empty);

        Assert.False(output.Heat);
        Assert.False(output.Cool);
        Assert.True(output.Fan);
    }

}
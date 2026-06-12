using HvacController.Models;
using HvacController.Services;

namespace HvacController.Tests;

public sealed class ThermostatEngineTests
{
    [Fact]
    public void CoolMode_WhenTemperatureIsAboveSetpoint_TurnsOnCoolingAndFan()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings());

        var input = new ThermostatInput
        {
            CurrentTempF = 74,
            CurrentHumidity = 48,
            SetpointF = 72,
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
    public void HeatMode_WhenTemperatureIsBelowSetpoint_TurnsOnHeatOnly()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings());

        var input = new ThermostatInput
        {
            CurrentTempF = 68,
            CurrentHumidity = 48,
            SetpointF = 70,
            Mode = HvacMode.Heat,
            Now = now,
            LastSensorUpdate = now
        };

        var (output, _) = engine.Evaluate(input, ThermostatRuntimeState.Empty);

        Assert.True(output.Heat);
        Assert.False(output.Cool);
        Assert.False(output.Fan);
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
            CurrentTempF = 80,
            CurrentHumidity = 48,
            SetpointF = 72,
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
    public void Cooling_WhenRecentlyStopped_DoesNotRestartBeforeMinimumOffTime()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            MinimumOffTime = TimeSpan.FromMinutes(5),
            IdleFanOn = false
        });

        var previousState = ThermostatRuntimeState.Empty with
        {
            WasCooling = false,
            LastCoolStopped = now.AddMinutes(-2)
        };

        var input = new ThermostatInput
        {
            CurrentTempF = 80,
            CurrentHumidity = 48,
            SetpointF = 72,
            Mode = HvacMode.Cool,
            Now = now,
            LastSensorUpdate = now
        };

        var (output, _) = engine.Evaluate(input, previousState);

        Assert.False(output.Cool);
        Assert.False(output.Heat);
        Assert.False(output.Fan);
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
            CurrentTempF = 71,
            CurrentHumidity = 48,
            SetpointF = 72,
            Mode = HvacMode.Cool,
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
        var engine = new ThermostatEngine(new HvacSettings
        {
            IdleFanOn = true
        });

        var input = new ThermostatInput
        {
            CurrentTempF = 72,
            CurrentHumidity = 48,
            SetpointF = 72,
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
        var engine = new ThermostatEngine(new HvacSettings
        {
            IdleFanOn = true
        });

        var input = new ThermostatInput
        {
            CurrentTempF = 70,
            CurrentHumidity = 48,
            SetpointF = 70,
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
            CurrentTempF = 71,
            CurrentHumidity = 48,
            SetpointF = 70,
            Mode = HvacMode.Heat,
            Now = now,
            LastSensorUpdate = now
        };

        var (output, _) = engine.Evaluate(input, previousState);

        Assert.True(output.Heat);
        Assert.False(output.Cool);
        Assert.False(output.Fan);
    }

    [Fact]
    public void Heat_WhenRecentlyStopped_DoesNotRestartBeforeMinimumOffTime()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            MinimumOffTime = TimeSpan.FromMinutes(5),
            IdleFanOn = false
        });

        var previousState = ThermostatRuntimeState.Empty with
        {
            WasHeating = false,
            LastHeatStopped = now.AddMinutes(-2)
        };

        var input = new ThermostatInput
        {
            CurrentTempF = 60,
            CurrentHumidity = 48,
            SetpointF = 70,
            Mode = HvacMode.Heat,
            Now = now,
            LastSensorUpdate = now
        };

        var (output, _) = engine.Evaluate(input, previousState);

        Assert.False(output.Heat);
        Assert.False(output.Cool);
        Assert.False(output.Fan);
    }
}
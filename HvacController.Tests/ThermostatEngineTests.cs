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
            MinimumCoolOffTime = TimeSpan.FromMinutes(5)
        });

        var previousState = ThermostatRuntimeState.Empty with
        {
            WasCooling = false,
            LastCoolStopped = now.AddMinutes(-2)
        };

        var input = new ThermostatInput
        {
            CurrentTempF = 80,
            SetpointF = 72,
            Mode = HvacMode.Cool,
            Now = now,
            LastSensorUpdate = now
        };

        var (output, _) = engine.Evaluate(input, previousState);

        Assert.False(output.Cool);
        Assert.False(output.Heat);
    }

    [Fact]
    public void Cooling_WhenAlreadyRunning_StaysOnUntilMinimumRunTimeSatisfied()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            MinimumCoolRunTime = TimeSpan.FromMinutes(5)
        });

        var previousState = ThermostatRuntimeState.Empty with
        {
            WasCooling = true,
            LastCoolStarted = now.AddMinutes(-2)
        };

        var input = new ThermostatInput
        {
            CurrentTempF = 71,
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
    public void Idle_WhenModeIsCoolAndNoCoolingNeeded_TurnsFanOn()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            IdleFanOn = true
        });

        var input = new ThermostatInput
        {
            CurrentTempF = 72,
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
    public void Idle_WhenModeIsHeatAndNoHeatingNeeded_TurnsFanOn()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            IdleFanOn = true
        });

        var input = new ThermostatInput
        {
            CurrentTempF = 70,
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
    public void IdleFan_WhenCoolingIsActive_FanStaysOnBecauseCoolingRequiresFan()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            IdleFanOn = true,
            FanOnWithCooling = true
        });

        var input = new ThermostatInput
        {
            CurrentTempF = 74,
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
    public void IdleFan_WhenHeatingIsActive_DoesNotForceFanOn()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var engine = new ThermostatEngine(new HvacSettings
        {
            IdleFanOn = true,
            FanOnWithHeat = false
        });

        var input = new ThermostatInput
        {
            CurrentTempF = 68,
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
}
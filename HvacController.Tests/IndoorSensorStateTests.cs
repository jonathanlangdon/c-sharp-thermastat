using HvacController.Models;
using HvacController.Services;

namespace HvacController.Tests;

public sealed class IndoorSensorStateTests
{
    [Fact]
    public void UpdateUpstairsSensor_StoresTemperatureHumidityAndSensorTime()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var state = new IndoorSensorState();

        state.UpdateUpstairsSensor(
            temperatureFahr: 72.1,
            relativeHumidity: 48.5,
            updatedAt: now);

        Assert.Equal(72.1, state.CurrentTempFahrUp);
        Assert.Equal(48.5, state.HumidityUpstairs);
        Assert.Equal(now, state.LastSensorUpdate);
    }

    [Fact]
    public void UpdateDownstairsSensor_StoresTemperatureHumidityAndSensorTime()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var state = new IndoorSensorState();

        state.UpdateDownstairsSensor(
            temperatureFahr: 66.4,
            relativeHumidity: 55.2,
            updatedAt: now);

        Assert.Equal(66.4, state.CurrentTempFahrDown);
        Assert.Equal(55.2, state.HumidityDownstairs);
        Assert.Equal(now, state.LastSensorUpdate);
    }

    [Fact]
    public void RecordMotion_StoresLastMotionDetected()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var state = new IndoorSensorState();

        state.RecordMotion(now);

        Assert.Equal(now, state.LastMotionDetected);
    }

    [Fact]
    public void SetMode_StoresMode()
    {
        var state = new IndoorSensorState();

        state.SetMode(HvacMode.Cool);

        Assert.Equal(HvacMode.Cool, state.Mode);
    }
}
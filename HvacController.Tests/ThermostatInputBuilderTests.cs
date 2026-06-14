using HvacController.Models;
using HvacController.Services;

namespace HvacController.Tests;

public sealed class ThermostatInputBuilderTests
{
    [Fact]
    public void Build_CopiesIndoorSensorStateIntoThermostatInput()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var indoorState = new IndoorSensorState();
        var outsideState = new OutsideWeatherState();

        indoorState.UpdateUpstairsSensor(
            temperatureFahr: 72.1,
            relativeHumidity: 48.5,
            updatedAt: now.AddSeconds(-10));

        indoorState.UpdateDownstairsSensor(
            temperatureFahr: 66.4,
            relativeHumidity: 55.2,
            updatedAt: now.AddSeconds(-8));

        indoorState.RecordMotion(now.AddMinutes(-30));
        indoorState.SetMode(HvacMode.Cool);

        var builder = new ThermostatInputBuilder(indoorState, outsideState);

        var input = builder.Build(now);

        Assert.Equal(72.1, input.CurrentTempFahrUp);
        Assert.Equal(66.4, input.CurrentTempFahrDown);
        Assert.Equal(48.5, input.HumidityUpstairs);
        Assert.Equal(55.2, input.HumidityDownstairs);
        Assert.Equal(now.AddSeconds(-8), input.LastSensorUpdate);
        Assert.Equal(now.AddMinutes(-30), input.LastMotionDetected);
        Assert.Equal(HvacMode.Cool, input.Mode);
        Assert.Equal(now, input.Now);
    }

    [Fact]
    public void Build_CopiesOutsideWeatherStateIntoThermostatInput()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var indoorState = new IndoorSensorState();
        var outsideState = new OutsideWeatherState();

        outsideState.Update(new OutsideWeatherReading
        {
            OutsideTemperature = 68.0,
            OutsideAbsoluteHumidity = 8.64,
            UpdatedAt = now.AddMinutes(-1)
        });

        var builder = new ThermostatInputBuilder(indoorState, outsideState);

        var input = builder.Build(now);

        Assert.Equal(8.64, input.OutsideAbsoluteHumidity);
    }
}
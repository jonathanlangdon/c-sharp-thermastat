using HvacController.Models;
using HvacController.Services;

namespace HvacController.Tests;

public sealed class ThermostatCycleRunnerTests
{
    [Fact]
    public void RunOnce_WhenCoolingIsNeeded_SendsCoolAndFanToRelays()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");

        var indoorState = new IndoorSensorState();
        indoorState.UpdateUpstairsSensor(
            temperatureFahr: 72,
            relativeHumidity: 50,
            updatedAt: now);
        indoorState.SetMode(HvacMode.Cool);

        var outsideState = new OutsideWeatherState();
        outsideState.Update(new OutsideWeatherReading
        {
            OutsideTemperature = 70,
            OutsideAbsoluteHumidity = 9.5,
            UpdatedAt = now
        });

        var inputBuilder = new ThermostatInputBuilder(indoorState, outsideState);
        var relays = new FakeRelayService();

        var runner = new ThermostatCycleRunner(
            inputBuilder,
            new HvacSettings(),
            relays);

        runner.RunOnce(now);

        Assert.False(relays.Heat);
        Assert.True(relays.Cool);
        Assert.True(relays.Fan);
    }

    [Fact]
    public void RunOnce_WhenNoTemperatureReading_SendsAllOffToRelays()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");

        var indoorState = new IndoorSensorState();
        var outsideState = new OutsideWeatherState();

        var inputBuilder = new ThermostatInputBuilder(indoorState, outsideState);
        var relays = new FakeRelayService();

        var runner = new ThermostatCycleRunner(
            inputBuilder,
            new HvacSettings(),
            relays);

        runner.RunOnce(now);

        Assert.False(relays.Heat);
        Assert.False(relays.Cool);
        Assert.False(relays.Fan);
    }

    private sealed class FakeRelayService : IRelayService
    {
        public bool Heat { get; private set; }
        public bool Cool { get; private set; }
        public bool Fan { get; private set; }

        public void AllOff()
        {
            Heat = false;
            Cool = false;
            Fan = false;
        }

        public void SetRelays(bool heat, bool cool, bool fan)
        {
            Heat = heat;
            Cool = cool;
            Fan = fan;
        }
    }
}

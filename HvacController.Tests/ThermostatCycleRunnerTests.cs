using HvacController.Models;
using HvacController.Services;

namespace HvacController.Tests;

public sealed class ThermostatCycleRunnerTests
{
    [Fact]
    public async Task RunOnce_WhenCoolingIsNeeded_SendsCoolAndFanToRelays()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");

        var indoorState = new IndoorSensorState();
        indoorState.UpdateUpstairsSensor(
            temperatureFahr: 72,
            relativeHumidity: 65,
            updatedAt: now);
        indoorState.SetMode(HvacMode.Cool);

        var outsideState = new OutsideWeatherState();
        outsideState.Update(new OutsideWeatherReading
        {
            OutsideTemperature = 70,
            OutsideAbsoluteHumidity = 12.5,
            UpdatedAt = now
        });

        var inputBuilder = new ThermostatInputBuilder(indoorState, outsideState);
        var settings = new HvacSettings();
        var relays = new FakeRelayService();
        var statusPublisher = new NoOpThermostatStatusPublisher();
        var persistentStateStore = new FakeThermostatPersistentStateStore
{
        State = new ThermostatPersistentState
        {
            Mode = HvacMode.Cool,
            HeatSetPointDay = 70.5,
            HeatSetPointNight = 65.0,
            HumidityTargetIdeal = 8.5,
            HumidityTargetGood = 10.2,
            HumidityTargetFair = 11.5
        }
};

        var runner = new ThermostatCycleRunner(
            inputBuilder,
            settings,
            relays,
            statusPublisher,
            outsideState,
            persistentStateStore);

        await runner.RunOnceAsync(now, CancellationToken.None);

        Assert.False(relays.Heat);
        Assert.True(relays.Cool);
        Assert.True(relays.Fan);
    }

    [Fact]
    public async Task RunOnce_WhenNoTemperatureReading_SendsAllOffToRelays()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");

        var indoorState = new IndoorSensorState();
        var outsideState = new OutsideWeatherState();

        var inputBuilder = new ThermostatInputBuilder(indoorState, outsideState);
        var settings = new HvacSettings();
        var relays = new FakeRelayService();
        var statusPublisher = new NoOpThermostatStatusPublisher();
        var persistentStateStore = new FakeThermostatPersistentStateStore();

        var runner = new ThermostatCycleRunner(
            inputBuilder,
            settings,
            relays,
            statusPublisher,
            outsideState,
            persistentStateStore);

        await runner.RunOnceAsync(now, CancellationToken.None);

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

    private sealed class FakeThermostatPersistentStateStore : IThermostatPersistentStateStore
    {
        public ThermostatPersistentState State { get; set; } = new();

        public ThermostatPersistentState Load()
        {
            return State;
        }

        public void Save(ThermostatPersistentState state)
        {
            State = state;
        }
    }
}
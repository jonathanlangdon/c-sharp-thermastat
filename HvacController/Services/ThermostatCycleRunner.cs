using HvacController.Models;

namespace HvacController.Services;

public sealed class ThermostatCycleRunner
{
    private readonly ThermostatInputBuilder _inputBuilder;
    private readonly ThermostatEngine _engine;
    private readonly IRelayService _relays;
    private readonly IThermostatStatusPublisher _statusPublisher;
    private readonly OutsideWeatherState _outsideWeatherState;
    private readonly IThermostatPersistentStateStore _persistentStateStore;

    private ThermostatRuntimeState _state;

    public ThermostatCycleRunner(
        ThermostatInputBuilder inputBuilder,
        HvacSettings settings,
        IRelayService relays,
        IThermostatStatusPublisher statusPublisher,
        OutsideWeatherState outsideWeatherState,
        IThermostatPersistentStateStore persistentStateStore)
    {
        _inputBuilder = inputBuilder;
        _engine = new ThermostatEngine(settings);
        _relays = relays;
        _statusPublisher = statusPublisher;
        _outsideWeatherState = outsideWeatherState;
        _persistentStateStore = persistentStateStore;

        _state = _persistentStateStore.Load().ToRuntimeState();
    }

    public async Task<ThermostatOutput> RunOnceAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var persisted = _persistentStateStore.Load();
        var rawInput = _inputBuilder.Build(now);

        var input = rawInput with
        {
            LastSensorUpdate =
                rawInput.LastSensorUpdate ?? persisted.LastSensorUpdate,

            LastMotionDetected =
                rawInput.LastMotionDetected ?? persisted.LastMotionDetected,

            Mode =
                rawInput.LastSensorUpdate is null
                    ? persisted.Mode
                    : rawInput.Mode,

            DayHeatSetPoint = persisted.HeatSetPointDay,
            NightHeatSetPoint = persisted.HeatSetPointNight,
            AbsoluteHumidityCoolingOnThreshold = persisted.MaxAbsHumSetPoint
        };

        var (output, newState) = _engine.Evaluate(
            input,
            _state);

        _state = newState;

        _persistentStateStore.Save(
            ThermostatPersistentState.From(
                input,
                _state));

        _relays.SetRelays(
            heat: output.Heat,
            cool: output.Cool,
            fan: output.Fan);

        var statusMessage = ThermostatStatusMessageBuilder.Build(
            input,
            output,
            _state,
            _outsideWeatherState.OutsideTemperature,
            _outsideWeatherState.LastUpdated);

        await _statusPublisher.PublishAsync(
            statusMessage,
            cancellationToken);

        return output;
    }
}
using HvacController.Models;

namespace HvacController.Services;

public sealed class ThermostatCycleRunner
{
    private readonly ThermostatInputBuilder _inputBuilder;
    private readonly ThermostatEngine _engine;
    private readonly IRelayService _relays;
    private readonly IThermostatStatusPublisher _statusPublisher;
    private readonly OutsideWeatherState _outsideWeatherState;

    private ThermostatRuntimeState _runtimeState = ThermostatRuntimeState.Empty;

    public ThermostatCycleRunner(
        ThermostatInputBuilder inputBuilder,
        HvacSettings settings,
        IRelayService relays,
        IThermostatStatusPublisher statusPublisher,
        OutsideWeatherState outsideWeatherState)
    {
        _inputBuilder = inputBuilder;
        _engine = new ThermostatEngine(settings);
        _relays = relays;
        _statusPublisher = statusPublisher;
        _outsideWeatherState = outsideWeatherState;
    }

    public async Task<ThermostatOutput> RunOnceAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var input = _inputBuilder.Build(now);

        var (output, newState) = _engine.Evaluate(
            input,
            _runtimeState);

        _runtimeState = newState;

        _relays.SetRelays(
            heat: output.Heat,
            cool: output.Cool,
            fan: output.Fan);

        var statusMessage = ThermostatStatusMessageBuilder.Build(
            input,
            output,
            _runtimeState,
            _outsideWeatherState.OutsideTemperature,
            _outsideWeatherState.LastUpdated);

        await _statusPublisher.PublishAsync(
            statusMessage,
            cancellationToken);

        return output;
    }
}
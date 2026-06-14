using HvacController.Models;

namespace HvacController.Services;

public sealed class ThermostatCycleRunner
{
    private readonly ThermostatInputBuilder _inputBuilder;
    private readonly ThermostatEngine _engine;
    private readonly IRelayService _relays;

    private ThermostatRuntimeState _runtimeState = ThermostatRuntimeState.Empty;

    public ThermostatCycleRunner(
        ThermostatInputBuilder inputBuilder,
        HvacSettings settings,
        IRelayService relays)
    {
        _inputBuilder = inputBuilder;
        _engine = new ThermostatEngine(settings);
        _relays = relays;
    }

    public ThermostatOutput RunOnce(DateTimeOffset now)
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

        return output;
    }
}

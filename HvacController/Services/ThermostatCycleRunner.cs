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
        var persistedWithRuntime = persisted.UpdateRuntimeTotals(now, _state);
        var rawInput = _inputBuilder.Build(now);

        var input = rawInput with
        {
            CurrentTempFahrUp =
                ApplyCalibration(rawInput.CurrentTempFahrUp, persisted.UpTempCalibration),

            RelHumidityUpstairs =
                ApplyRelativeHumidityCalibration(rawInput.RelHumidityUpstairs, persisted.UpRelHumCalibration),

            CurrentTempFahrDown =
                ApplyCalibration(rawInput.CurrentTempFahrDown, persisted.DownTempCalibration),

            RelHumidityDownstairs =
                ApplyRelativeHumidityCalibration(rawInput.RelHumidityDownstairs, persisted.DownRelHumCalibration),

            LastSensorUpdate =
                rawInput.LastSensorUpdate ?? persisted.LastSensorUpdate,

            LastMotionDetected =
                rawInput.LastMotionDetected ?? persisted.LastMotionDetected,

            Mode = persisted.Mode,

            DayHeatSetPoint = persisted.HeatSetPointDay,
            NightHeatSetPoint = persisted.HeatSetPointNight,
            
            HumidityTargetIdeal = persisted.HumidityTargetIdeal,
            HumidityTargetGood = persisted.HumidityTargetGood,
            HumidityTargetFair = persisted.HumidityTargetFair,

            UpTempCalibration = persisted.UpTempCalibration,
            UpRelHumCalibration = persisted.UpRelHumCalibration,
            DownTempCalibration = persisted.DownTempCalibration,
            DownRelHumCalibration = persisted.DownRelHumCalibration
        };

        var (output, newState) = _engine.Evaluate(
            input,
            _state);

        _state = newState;

        var stateToSave = ThermostatPersistentState.From(
            input,
            _state) with
        {
            RuntimeDate = persistedWithRuntime.RuntimeDate,
            CoolHoursToday = persistedWithRuntime.CoolHoursToday,
            HeatHoursToday = persistedWithRuntime.HeatHoursToday,
            LastRuntimeUpdated = persistedWithRuntime.LastRuntimeUpdated
        };

        _persistentStateStore.Save(stateToSave);

        _relays.SetRelays(
            heat: output.Heat,
            cool: output.Cool,
            fan: output.Fan);

        var statusMessage = ThermostatStatusMessageBuilder.Build(
            input,
            output,
            _state,
            _outsideWeatherState.OutsideTemperature,
            _outsideWeatherState.LastUpdated,
            stateToSave.CoolHoursToday,
            stateToSave.HeatHoursToday);
        

        await _statusPublisher.PublishAsync(
            statusMessage,
            cancellationToken);

        return output;
    }

    private static double? ApplyCalibration(
        double? value,
        double calibration)
    {
        return value is null
            ? null
            : value.Value + calibration;
    }

    private static double? ApplyRelativeHumidityCalibration(
        double? value,
        double calibration)
    {
        return value is null
            ? null
            : Math.Clamp(value.Value + calibration, 0.0, 100.0);
    }

}
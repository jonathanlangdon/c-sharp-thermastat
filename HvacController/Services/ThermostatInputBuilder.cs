using HvacController.Models;

namespace HvacController.Services;

public sealed class ThermostatInputBuilder
{
    private readonly IndoorSensorState _indoorState;
    private readonly OutsideWeatherState _outsideState;

    public ThermostatInputBuilder(
        IndoorSensorState indoorState,
        OutsideWeatherState outsideState)
    {
        _indoorState = indoorState;
        _outsideState = outsideState;
    }

    public ThermostatInput Build(DateTimeOffset now)
    {
        return new ThermostatInput
        {
            CurrentTempFahrUp = _indoorState.CurrentTempFahrUp,
            CurrentTempFahrDown = _indoorState.CurrentTempFahrDown,

            RelHumidityUpstairs = _indoorState.HumidityUpstairs,
            RelHumidityDownstairs = _indoorState.HumidityDownstairs,

            OutsideAbsoluteHumidity = _outsideState.OutsideAbsoluteHumidity,

            Mode = _indoorState.Mode,
            Now = now,
            LastSensorUpdate = _indoorState.LastSensorUpdate,
            LastMotionDetected = _indoorState.LastMotionDetected
        };
    }
}
using HvacController.Models;

namespace HvacController.Services;

public static class ThermostatStatusMessageBuilder
{
    public static ThermostatStatusMessage Build(
        ThermostatInput input,
        ThermostatOutput output,
        ThermostatRuntimeState runtimeState,
        double? outsideTemperature = null,
        DateTimeOffset? outsideWeatherUpdatedAt = null)
    {
        return new ThermostatStatusMessage
        {
            Now = input.Now,
            LastSensorUpdate = input.LastSensorUpdate,
            LastMotionDetected = input.LastMotionDetected,
            OutsideWeatherUpdatedAt = outsideWeatherUpdatedAt,

            Heat = output.Heat,
            Cool = output.Cool,
            Fan = output.Fan,
            Reason = output.Reason,

            Mode = input.Mode,

            UpstairsTemperature = input.CurrentTempFahrUp,
            UpstairsRelativeHumidity = input.HumidityUpstairs,
            UpstairsAbsoluteHumidity = input.AbsoluteHumidityUpstairs,

            DownstairsTemperature = input.CurrentTempFahrDown,
            DownstairsRelativeHumidity = input.HumidityDownstairs,
            DownstairsAbsoluteHumidity = input.AbsoluteHumidityDownstairs,

            ControlAbsoluteHumidity = input.ControlAbsoluteHumidity,

            OutsideTemperature = outsideTemperature,
            OutsideAbsoluteHumidity = input.OutsideAbsoluteHumidity,

            WasHeating = runtimeState.WasHeating,
            WasCooling = runtimeState.WasCooling,
            LastHeatStarted = runtimeState.LastHeatStarted,
            LastHeatStopped = runtimeState.LastHeatStopped,
            LastCoolStarted = runtimeState.LastCoolStarted,
            LastCoolStopped = runtimeState.LastCoolStopped,
        };
    }
}
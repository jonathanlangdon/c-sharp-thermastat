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
            HeatSetPointFahr = output.HeatSetPointFahr,
            HeatSetPointDay = input.DayHeatSetPoint,
            HeatSetPointNight = input.NightHeatSetPoint,
            MaxAbsHumSetPoint = input.AbsoluteHumidityCoolingOnThreshold,

            UpstairsTemperature = input.CurrentTempFahrUp,
            UpstairsRelativeHumidity = input.HumidityUpstairs,
            UpstairsAbsoluteHumidity = input.AbsoluteHumidityUpstairs,

            DownstairsTemperature = input.CurrentTempFahrDown,
            DownstairsRelativeHumidity = input.HumidityDownstairs,
            DownstairsAbsoluteHumidity = input.AbsoluteHumidityDownstairs,

            ControlAbsoluteHumidity = input.ControlHumidity,

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
using HvacController.Models;

namespace HvacController.Services;

public static class ThermostatStatusMessageBuilder
{
    
    public static ThermostatStatusMessage Build(
        ThermostatInput input,
        ThermostatOutput output,
        ThermostatRuntimeState runtimeState,
        double? outsideTemperature = null,
        DateTimeOffset? outsideWeatherUpdatedAt = null,
        double coolHoursToday = 0,
        double heatHoursToday = 0)
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
            MaxAbsHumSetPoint = output.MaxAbsHumSetPoint,

            UpTempCalibration = input.UpTempCalibration,
            UpRelHumCalibration = input.UpRelHumCalibration,
            DownTempCalibration = input.DownTempCalibration,
            DownRelHumCalibration = input.DownRelHumCalibration,

            UpstairsTemperature = input.CurrentTempFahrUp,
            UpstairsRelativeHumidity = input.RelHumidityUpstairs,
            UpstairsAbsoluteHumidity = input.AbsoluteHumidityUpstairs,

            DownstairsTemperature = input.CurrentTempFahrDown,
            DownstairsRelativeHumidity = input.RelHumidityDownstairs,
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
            CoolHoursToday = Math.Round(coolHoursToday, 2),
            HeatHoursToday = Math.Round(heatHoursToday, 2),
        };
    }
}
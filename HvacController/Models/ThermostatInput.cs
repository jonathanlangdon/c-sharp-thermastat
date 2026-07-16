using HvacController.Services;

namespace HvacController.Models;

public sealed record ThermostatInput
{
    public double? CurrentTempFahrUp { get; init; }
    public double? CurrentTempFahrDown { get; init; }

    public double? RelHumidityUpstairs { get; init; }
    public double? RelHumidityDownstairs { get; init; }

    public double? OutsideAbsoluteHumidity { get; init; }

    public DateTime CurrentLocalTime { get; set; } = DateTime.Now;

    public double DayHeatSetPoint { get; init; }
    public double NightHeatSetPoint { get; init; }
    public double AbsoluteHumidityCoolingOnThreshold { get; init; }

    public double UpTempCalibration { get; init; }
    public double UpRelHumCalibration { get; init; }
    public double DownTempCalibration { get; init; }
    public double DownRelHumCalibration { get; init; }

    public double? AbsoluteHumidityUpstairs =>
        CurrentTempFahrUp is null || RelHumidityUpstairs is null
            ? null
            : AbsoluteHumidityCalculator.CalculateGramsPerCubicMeterFromFahrenheit(
                CurrentTempFahrUp.Value,
                RelHumidityUpstairs.Value);

    public double? AbsoluteHumidityDownstairs =>
        CurrentTempFahrDown is null || RelHumidityDownstairs is null
            ? null
            : AbsoluteHumidityCalculator.CalculateGramsPerCubicMeterFromFahrenheit(
                CurrentTempFahrDown.Value,
                RelHumidityDownstairs.Value);

    public double? ControlHumidity
    {
        get
        {
            var values = new[]
            {
                AbsoluteHumidityUpstairs,
                AbsoluteHumidityDownstairs
            }
            .Where(value => value is not null)
            .Select(value => value!.Value)
            .ToArray();

            return values.Length == 0
                ? null
                : values.Max();
        }
    }

    public HvacMode Mode { get; init; } = HvacMode.Heat;

    public DateTimeOffset Now { get; init; }
    public DateTimeOffset? LastSensorUpdate { get; init; }
    public DateTimeOffset? LastMotionDetected { get; init; }
}
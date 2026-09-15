using HvacController.Services;

namespace HvacController.Models;

public sealed record ThermostatInput
{
    public double? CurrentTempFahrUp { get; init; }
    public double? CurrentTempFahrDown { get; init; }

    public double? RelHumidityUpstairs { get; init; }
    public double? RelHumidityDownstairs { get; init; }

    public double? OutsideAbsoluteHumidity { get; init; }
    public double? OutsideTemperature { get; init; }

    public DateTime CurrentLocalTime { get; set; } = DateTime.Now;

    public double DayHeatSetPoint { get; init; }
    public double NightHeatSetPoint { get; init; }

    public double HumidityTargetIdeal { get; init; }
    public double HumidityTargetGood { get; init; }
    public double HumidityTargetFair { get; init; }

    public bool ManualOverride { get; init; }
    public ManualMode ManualMode { get; init; }

    public double UpTempCalibration { get; init; }
    public double UpRelHumCalibration { get; init; }
    public double DownTempCalibration { get; init; }
    public double DownRelHumCalibration { get; init; }

    public HvacMode Mode { get; init; }

    public DateTimeOffset Now { get; init; }
    public DateTimeOffset? LastSensorUpdate { get; init; }
    public DateTimeOffset? LastMotionDetected { get; init; }

    public double? DehumidSetUp =>
        CurrentTempFahrUp is null
            ? null
            : RoundUpToNearestFivePercent(
                AbsoluteHumidityCalculator.CalculateRelativeHumidityFromGramsPerCubicMeterAndFahrenheit(
                    HumidityTargetIdeal,
                    CurrentTempFahrUp.Value));

    public double? DehumidSetDown =>
        CurrentTempFahrDown is null
            ? null
            : RoundUpToNearestFivePercent(
                AbsoluteHumidityCalculator.CalculateRelativeHumidityFromGramsPerCubicMeterAndFahrenheit(
                    HumidityTargetIdeal,
                    CurrentTempFahrDown.Value));

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

    // round to closest 5 percent for dehumidifier settings -> special rounding
    // ex) 41.5 rounds to 40 while 41.6 rounds to 45
    private static double RoundUpToNearestFivePercent(double value)
    {
        var rounded = Math.Ceiling((value - 1.5)/ 5.0) * 5.0;
        return Math.Clamp(rounded, 0.0, 100.0);
    }

    public bool ShouldOpenWindows
    {
        get
        {
            var isDaytime =
                Now.TimeOfDay >= TimeSpan.FromHours(7) &&
                Now.TimeOfDay < TimeSpan.FromHours(22);

            var conditionOne =
                OutsideAbsoluteHumidity is not null &&
                OutsideAbsoluteHumidity.Value < (HumidityTargetIdeal - .5) && // nice humidity out
                CurrentTempFahrUp is not null &&
                CurrentTempFahrUp.Value > NightHeatSetPoint && // not too cold inside
                OutsideTemperature is not null &&
                OutsideTemperature.Value > (NightHeatSetPoint - 5); // not too cold outside


            var conditionTwo =
                OutsideAbsoluteHumidity is not null &&
                OutsideAbsoluteHumidity.Value < (HumidityTargetIdeal - .5) && // nice humidity out
                CurrentTempFahrUp is not null &&
                CurrentTempFahrUp.Value >= DayHeatSetPoint; // inside temp is warm (probably >= 70.5)

            return isDaytime && (conditionOne || conditionTwo);
        }
    }

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

}
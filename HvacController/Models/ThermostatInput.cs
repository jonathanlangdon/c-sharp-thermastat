using HvacController.Services;

namespace HvacController.Models;

public sealed record ThermostatInput
{
    public double? CurrentTempFahrUp { get; init; }
    public double? CurrentTempFahrDown { get; init; }

    public double? HumidityUpstairs { get; init; }
    public double? HumidityDownstairs { get; init; }

    public double? OutsideAbsoluteHumidity { get; init; }

    public double? AbsoluteHumidityUpstairs =>
        CurrentTempFahrUp is null || HumidityUpstairs is null
            ? null
            : AbsoluteHumidityCalculator.CalculateGramsPerCubicMeterFromFahrenheit(
                CurrentTempFahrUp.Value,
                HumidityUpstairs.Value);

    public double? AbsoluteHumidityDownstairs =>
        CurrentTempFahrDown is null || HumidityDownstairs is null
            ? null
            : AbsoluteHumidityCalculator.CalculateGramsPerCubicMeterFromFahrenheit(
                CurrentTempFahrDown.Value,
                HumidityDownstairs.Value);

    public double? ControlAbsoluteHumidity
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
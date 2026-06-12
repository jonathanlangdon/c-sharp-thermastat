namespace HvacController.Services;

public static class AbsoluteHumidityCalculator
{
    public static double CalculateGramsPerCubicMeter(
        double temperatureC,
        double relativeHumidity)
    {
        var absoluteHumidity =
            (6.112
             * Math.Exp((17.67 * temperatureC) / (temperatureC + 243.5))
             * relativeHumidity
             * 2.1674)
            / (273.15 + temperatureC);

        return Math.Round(absoluteHumidity, 2);
    }

    public static double CalculateGramsPerCubicMeterFromFahrenheit(
        double temperatureF,
        double relativeHumidity)
    {
        var temperatureC = (temperatureF - 32) * 5 / 9;

        return CalculateGramsPerCubicMeter(
            temperatureC,
            relativeHumidity);
    }
}

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

    public static double CalculateRelativeHumidityFromGramsPerCubicMeterAndFahrenheit(
        double absoluteHumidity,
        double temperatureFahr)
    {
        var temperatureC = (temperatureFahr - 32.0) * 5.0 / 9.0;
        var temperatureK = temperatureC + 273.15;

        var saturationVaporPressure =
            6.112 * Math.Exp((17.67 * temperatureC) / (temperatureC + 243.5));

        var actualVaporPressure =
            absoluteHumidity * temperatureK / 216.7;

        var relativeHumidity =
            actualVaporPressure / saturationVaporPressure * 100.0;

        return Math.Clamp(relativeHumidity, 0.0, 100.0);
    }

}

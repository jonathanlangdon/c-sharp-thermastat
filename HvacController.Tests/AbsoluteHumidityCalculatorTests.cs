using HvacController.Services;

namespace HvacController.Tests;

public sealed class AbsoluteHumidityCalculatorTests
{
    [Fact]
    public void CalculateGramsPerCubicMeter_With20CAnd50PercentHumidity_Returns8Point64()
    {
        var result = AbsoluteHumidityCalculator.CalculateGramsPerCubicMeter(
            temperatureC: 20,
            relativeHumidity: 50);

        Assert.Equal(8.64, result, precision: 2);
    }

    [Fact]
    public void CalculateGramsPerCubicMeter_With25CAnd50PercentHumidity_Returns11Point51()
    {
        var result = AbsoluteHumidityCalculator.CalculateGramsPerCubicMeter(
            temperatureC: 25,
            relativeHumidity: 50);

        Assert.Equal(11.51, result, precision: 2);
    }

    [Fact]
    public void CalculateGramsPerCubicMeter_With72FAnd50PercentHumidity_Returns9Point69()
    {
        var result = AbsoluteHumidityCalculator.CalculateGramsPerCubicMeterFromFahrenheit(
            temperatureF: 72,
            relativeHumidity: 50);

        Assert.Equal(9.83, result, precision: 2);
    }
}

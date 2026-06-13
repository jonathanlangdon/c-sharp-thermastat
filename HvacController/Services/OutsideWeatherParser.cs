using System.Text.Json;
using HvacController.Models;

namespace HvacController.Services;

public static class OutsideWeatherParser
{
    public static OutsideWeatherReading? Parse(string json)
    {
        using var document = JsonDocument.Parse(json);

        var properties = document.RootElement.GetProperty("properties");

        var temperatureC = properties
            .GetProperty("temperature")
            .GetProperty("value");

        var relativeHumidity = properties
            .GetProperty("relativeHumidity")
            .GetProperty("value");

        if (temperatureC.ValueKind is JsonValueKind.Null ||
            relativeHumidity.ValueKind is JsonValueKind.Null)
        {
            return null;
        }

        var temperatureCValue = temperatureC.GetDouble();
        var relativeHumidityValue = relativeHumidity.GetDouble();

        var outsideTemperatureF = Math.Round(
            temperatureCValue * 9 / 5 + 32,
            1);

        var outsideAbsoluteHumidity =
            AbsoluteHumidityCalculator.CalculateGramsPerCubicMeter(
                temperatureCValue,
                relativeHumidityValue);

        return new OutsideWeatherReading
        {
            OutsideTemperature = outsideTemperatureF,
            OutsideAbsoluteHumidity = outsideAbsoluteHumidity,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
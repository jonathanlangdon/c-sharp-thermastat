using HvacController.Services;

namespace HvacController.Tests;

public sealed class OutsideWeatherParserTests
{
    [Fact]
    public void Parse_WhenTemperatureAndHumidityExist_ReturnsOutsideWeather()
    {
        var json = """
        {
          "properties": {
            "temperature": {
              "value": 20.0
            },
            "relativeHumidity": {
              "value": 50.0
            }
          }
        }
        """;

        var result = OutsideWeatherParser.Parse(json);

        Assert.NotNull(result);
        Assert.Equal(68.0, result.OutsideTemperature);
        Assert.Equal(8.64, result.OutsideAbsoluteHumidity);
    }

    [Fact]
    public void Parse_WhenTemperatureIsMissing_ReturnsNull()
    {
        var json = """
        {
          "properties": {
            "temperature": {
              "value": null
            },
            "relativeHumidity": {
              "value": 50.0
            }
          }
        }
        """;

        var result = OutsideWeatherParser.Parse(json);

        Assert.Null(result);
    }

    [Fact]
    public void Parse_WhenHumidityIsMissing_ReturnsNull()
    {
        var json = """
        {
          "properties": {
            "temperature": {
              "value": 20.0
            },
            "relativeHumidity": {
              "value": null
            }
          }
        }
        """;

        var result = OutsideWeatherParser.Parse(json);

        Assert.Null(result);
    }
}
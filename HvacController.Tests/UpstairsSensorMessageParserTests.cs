using HvacController.Models;
using HvacController.Services;

namespace HvacController.Tests;

public sealed class UpstairsSensorMessageParserTests
{
    [Fact]
    public void Parse_WhenPayloadIsValid_ReturnsMessage()
    {
        var json = """
        {
          "temperatureFahr": 72.1,
          "relativeHumidity": 48.5,
          "motionDetected": true,
          "mode": "Cool"
        }
        """;

        var result = UpstairsSensorMessageParser.Parse(json);

        Assert.NotNull(result);
        Assert.Equal(72.1, result.TemperatureFahr);
        Assert.Equal(48.5, result.RelativeHumidity);
        Assert.True(result.MotionDetected);
        Assert.Equal(HvacMode.Cool, result.Mode);
    }

    [Fact]
    public void Parse_WhenMotionDetectedIsMissing_ReturnsMessage()
    {
        var json = """
        {
          "temperatureFahr": 72.1,
          "relativeHumidity": 48.5,
          "mode": "Heat"
        }
        """;

        var result = UpstairsSensorMessageParser.Parse(json);

        Assert.NotNull(result);
        Assert.Equal(72.1, result.TemperatureFahr);
        Assert.Equal(48.5, result.RelativeHumidity);
        Assert.Null(result.MotionDetected);
        Assert.Equal(HvacMode.Heat, result.Mode);
    }

    [Fact]
    public void Parse_WhenModeIsMissing_ReturnsMessage()
    {
        var json = """
        {
          "temperatureFahr": 72.1,
          "relativeHumidity": 48.5,
          "motionDetected": false
        }
        """;

        var result = UpstairsSensorMessageParser.Parse(json);

        Assert.NotNull(result);
        Assert.Equal(72.1, result.TemperatureFahr);
        Assert.Equal(48.5, result.RelativeHumidity);
        Assert.False(result.MotionDetected);
        Assert.Null(result.Mode);
    }

    [Fact]
    public void Parse_WhenModeIsInvalid_ReturnsNull()
    {
        var json = """
        {
          "temperatureFahr": 72.1,
          "relativeHumidity": 48.5,
          "motionDetected": true,
          "mode": "Auto"
        }
        """;

        var result = UpstairsSensorMessageParser.Parse(json);

        Assert.Null(result);
    }

    [Fact]
    public void Parse_WhenRequiredTemperatureIsMissing_ReturnsNull()
    {
        var json = """
        {
          "relativeHumidity": 48.5,
          "motionDetected": true,
          "mode": "Cool"
        }
        """;

        var result = UpstairsSensorMessageParser.Parse(json);

        Assert.Null(result);
    }

    [Fact]
    public void Parse_WhenRequiredHumidityIsMissing_ReturnsNull()
    {
        var json = """
        {
          "temperatureFahr": 72.1,
          "motionDetected": true,
          "mode": "Cool"
        }
        """;

        var result = UpstairsSensorMessageParser.Parse(json);

        Assert.Null(result);
    }

    [Fact]
    public void Parse_WhenJsonIsInvalid_ReturnsNull()
    {
        var result = UpstairsSensorMessageParser.Parse("not json");

        Assert.Null(result);
    }
}

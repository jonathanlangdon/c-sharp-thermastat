using System.Text.Json;
using HvacController.Models;
using HvacController.Services;

namespace HvacController.Tests;

public sealed class ThermostatStatusMessageSerializerTests
{
    [Fact]
    public void Serialize_UsesCamelCaseJson()
    {
        var message = new ThermostatStatusMessage
        {
            Heat = false,
            Cool = true,
            Fan = true,
            Reason = "Everything Normal",
            Mode = HvacMode.Cool,
            UpstairsTemperature = 72.1,
            UpstairsRelativeHumidity = 50.0,
            UpstairsAbsoluteHumidity = 9.83,
            DownstairsTemperature = 66.4,
            DownstairsRelativeHumidity = 55.2,
            DownstairsAbsoluteHumidity = 8.16,
            ControlAbsoluteHumidity = 9.83,
            OutsideTemperature = 70.0,
            OutsideAbsoluteHumidity = 9.4
        };

        var json = ThermostatStatusMessageSerializer.Serialize(message);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.True(root.GetProperty("cool").GetBoolean());
        Assert.True(root.GetProperty("fan").GetBoolean());
        Assert.Equal("Everything Normal", root.GetProperty("reason").GetString());
        Assert.Equal("Cool", root.GetProperty("mode").GetString());
        Assert.Equal(72.1, root.GetProperty("upstairsTemperature").GetDouble());
        Assert.Equal(9.83, root.GetProperty("upstairsAbsoluteHumidity").GetDouble());
        Assert.Equal(8.16, root.GetProperty("downstairsAbsoluteHumidity").GetDouble());
        Assert.Equal(9.83, root.GetProperty("controlAbsoluteHumidity").GetDouble());
        Assert.Equal(9.4, root.GetProperty("outsideAbsoluteHumidity").GetDouble());
    }

    [Fact]
    public void Serialize_TruncatesDateTimesAndConvertsThemToEasternTime()
    {

        var message = new ThermostatStatusMessage
        {
            Now = new DateTimeOffset(
                2026, 6, 19, 15, 26, 34, 296,
                TimeSpan.FromHours(-4)),

            LastSensorUpdate = new DateTimeOffset(
                2026, 6, 19, 15, 26, 26, 886,
                TimeSpan.FromHours(-4)),

            LastMotionDetected = new DateTimeOffset(
                2026, 6, 19, 15, 26, 4, 497,
                TimeSpan.FromHours(-4)),

            OutsideWeatherUpdatedAt = new DateTimeOffset(
                2026, 6, 19, 19, 21, 45, 329,
                TimeSpan.Zero)
        };

        var json = ThermostatStatusMessageSerializer.Serialize(message);

        Assert.Contains("\"now\":\"2026-06-19T15:26:34\"", json);
        Assert.Contains("\"lastSensorUpdate\":\"2026-06-19T15:26:26\"", json);
        Assert.Contains("\"lastMotionDetected\":\"2026-06-19T15:26:04\"", json);
        Assert.Contains("\"outsideWeatherUpdatedAt\":\"2026-06-19T15:21:45\"", json);

        Assert.DoesNotContain(".296", json);
        Assert.DoesNotContain(".886", json);
        Assert.DoesNotContain(".497", json);
        Assert.DoesNotContain(".329", json);
        Assert.DoesNotContain("-04:00", json);
        Assert.DoesNotContain("+00:00", json);
    }

}

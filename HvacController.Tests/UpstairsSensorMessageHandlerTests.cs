using HvacController.Models;
using HvacController.Services;

namespace HvacController.Tests;

public sealed class UpstairsSensorMessageHandlerTests
{
    [Fact]
    public void Handle_WhenPayloadIsValid_UpdatesUpstairsSensorState()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var state = new IndoorSensorState();
        var handler = new UpstairsSensorMessageHandler(state);

        var json = """
        {
          "temperatureFahr": 72.1,
          "relativeHumidity": 48.5,
          "motionDetected": false,
          "mode": "Cool"
        }
        """;

        var handled = handler.Handle(json, now);

        Assert.True(handled);
        Assert.Equal(72.1, state.CurrentTempFahrUp);
        Assert.Equal(48.5, state.HumidityUpstairs);
        Assert.Equal(now, state.LastSensorUpdate);
        Assert.Equal(HvacMode.Cool, state.Mode);
        Assert.Null(state.LastMotionDetected);
    }

    [Fact]
    public void Handle_WhenMotionDetectedIsTrue_RecordsMotion()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var state = new IndoorSensorState();
        var handler = new UpstairsSensorMessageHandler(state);

        var json = """
        {
          "temperatureFahr": 72.1,
          "relativeHumidity": 48.5,
          "motionDetected": true,
          "mode": "Heat"
        }
        """;

        var handled = handler.Handle(json, now);

        Assert.True(handled);
        Assert.Equal(now, state.LastMotionDetected);
    }

    [Fact]
    public void Handle_WhenModeIsMissing_DoesNotChangeExistingMode()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var state = new IndoorSensorState();
        state.SetMode(HvacMode.Heat);

        var handler = new UpstairsSensorMessageHandler(state);

        var json = """
        {
          "temperatureFahr": 72.1,
          "relativeHumidity": 48.5,
          "motionDetected": false
        }
        """;

        var handled = handler.Handle(json, now);

        Assert.True(handled);
        Assert.Equal(HvacMode.Heat, state.Mode);
    }

    [Fact]
    public void Handle_WhenPayloadIsInvalid_ReturnsFalseAndDoesNotUpdateState()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");
        var state = new IndoorSensorState();
        var handler = new UpstairsSensorMessageHandler(state);

        var handled = handler.Handle("not json", now);

        Assert.False(handled);
        Assert.Null(state.CurrentTempFahrUp);
        Assert.Null(state.HumidityUpstairs);
        Assert.Null(state.LastSensorUpdate);
    }
}

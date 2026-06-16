using HvacController.Models;
using HvacController.Services;

namespace HvacController.Tests.Services;

public sealed class UpstairsSensorCsvFormatterTests
{
    [Fact]
    public void Header_ReturnsExpectedColumns()
    {
        var header = UpstairsSensorCsvFormatter.Header;

        Assert.Equal(
            "timestamp,temperatureFahr,relativeHumidity,motionDetected,mode",
            header);
    }

    [Fact]
    public void Format_ReturnsCsvLineForCompleteMessage()
    {
        var timestamp = new DateTimeOffset(
            2026, 6, 16, 14, 32, 10,
            TimeSpan.FromHours(-4));

        var message = new UpstairsSensorMessage
        {
            TemperatureFahr = 67.8,
            RelativeHumidity = 62.6,
            MotionDetected = true,
            Mode = HvacMode.Cool
        };

        var line = UpstairsSensorCsvFormatter.Format(timestamp, message);

        Assert.Equal(
            "2026-06-16T14:32:10.0000000-04:00,67.8,62.6,true,Cool",
            line);
    }

    [Fact]
    public void Format_LeavesOptionalValuesBlankWhenMissing()
    {
        var timestamp = new DateTimeOffset(
            2026, 6, 16, 14, 32, 10,
            TimeSpan.FromHours(-4));

        var message = new UpstairsSensorMessage
        {
            TemperatureFahr = 67.8,
            RelativeHumidity = 62.6,
            MotionDetected = null,
            Mode = null
        };

        var line = UpstairsSensorCsvFormatter.Format(timestamp, message);

        Assert.Equal(
            "2026-06-16T14:32:10.0000000-04:00,67.8,62.6,,",
            line);
    }
}

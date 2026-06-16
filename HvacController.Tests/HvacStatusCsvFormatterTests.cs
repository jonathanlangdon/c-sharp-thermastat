using HvacController.Models;
using HvacController.Services;

namespace HvacController.Tests;

public sealed class HvacStatusCsvFormatterTests
{
    [Fact]
    public void Header_ReturnsExpectedColumns()
    {
        var header = HvacStatusCsvFormatter.Header;

        Assert.Equal(
            "timestamp,heat,cool,fan,mode,upstairsTemperature,upstairsAbsoluteHumidity,downstairsTemperature,downstairsAbsoluteHumidity,outsideTemperature,outsideAbsoluteHumidity,reason",
            header);
    }

    [Fact]
    public void Format_ReturnsCsvLineForCompleteMessage()
    {
        var timestamp = new DateTimeOffset(
            2026, 6, 16, 14, 32, 10,
            TimeSpan.FromHours(-4));

        var message = new ThermostatStatusMessage
        {
            Heat = false,
            Cool = true,
            Fan = true,
            Mode = HvacMode.Cool,
            UpstairsTemperature = 76.1,
            UpstairsAbsoluteHumidity = 11.19,
            DownstairsTemperature = 72.0,
            DownstairsAbsoluteHumidity = 9.3,
            OutsideTemperature = 80.0,
            OutsideAbsoluteHumidity = 9.23,
            Reason = "Everything Normal"
        };

        var line = HvacStatusCsvFormatter.Format(timestamp, message);

        Assert.Equal(
            "2026-06-16T14:32:10.0000000-04:00,false,true,true,Cool,76.1,11.19,72.0,9.30,80.0,9.23,Everything Normal",
            line);
    }

    [Fact]
    public void Format_LeavesOptionalValuesBlankWhenMissing()
    {
        var timestamp = new DateTimeOffset(
            2026, 6, 16, 14, 32, 10,
            TimeSpan.FromHours(-4));

        var message = new ThermostatStatusMessage
        {
            Heat = false,
            Cool = false,
            Fan = false,
            Mode = HvacMode.Heat,
            Reason = "Sensor timeout"
        };

        var line = HvacStatusCsvFormatter.Format(timestamp, message);

        Assert.Equal(
            "2026-06-16T14:32:10.0000000-04:00,false,false,false,Heat,,,,,,,Sensor timeout",
            line);
    }
}

using HvacController.Models;
using HvacController.Services;

namespace HvacController.Tests;

public sealed class HvacStatusCsvFormatterTests
{
    [Fact]
    public void Header_ReturnsExpectedColumns()
    {
        Assert.Equal(
            "receivedAt,now,lastSensorUpdate,lastMotionDetected,outsideWeatherUpdatedAt," +
            "heat,cool,fan,reason,mode," +
            "heatSetPointFahr,heatSetPointDay,heatSetPointNight,maxAbsHumSetPoint," +
            "upTempCalibration,upRelHumCalibration,downTempCalibration,downRelHumCalibration," +
            "upstairsTemperature,upstairsRelativeHumidity,upstairsAbsoluteHumidity," +
            "downstairsTemperature,downstairsRelativeHumidity,downstairsAbsoluteHumidity," +
            "controlAbsoluteHumidity,outsideTemperature,outsideAbsoluteHumidity," +
            "wasHeating,wasCooling,lastHeatStarted,lastHeatStopped,lastCoolStarted,lastCoolStopped",
            HvacStatusCsvFormatter.Header);
    }

    [Fact]
    public void Format_ReturnsCsvLineForCompleteMessage()
    {
        var timestamp = DateTimeOffset.Parse("2026-06-29T18:32:10-04:00");

        var message = new ThermostatStatusMessage
        {
            Now = timestamp,
            LastSensorUpdate = timestamp.AddSeconds(-10),
            LastMotionDetected = timestamp.AddMinutes(-5),
            OutsideWeatherUpdatedAt = timestamp.AddMinutes(-2),

            Heat = false,
            Cool = true,
            Fan = true,
            Reason = "Everything Normal",
            Mode = HvacMode.Cool,

            HeatSetPointFahr = 65,
            HeatSetPointDay = 70.5,
            HeatSetPointNight = 65,
            MaxAbsHumSetPoint = 10.5,

            UpTempCalibration = -4.5,
            UpRelHumCalibration = 3,
            DownTempCalibration = -0.5,
            DownRelHumCalibration = -2,

            UpstairsTemperature = 69.8,
            UpstairsRelativeHumidity = 56.5,
            UpstairsAbsoluteHumidity = 10.35,

            DownstairsTemperature = 63.900000000000006,
            DownstairsRelativeHumidity = 63.099999999999994,
            DownstairsAbsoluteHumidity = 9.53,

            ControlAbsoluteHumidity = 10.35,
            OutsideTemperature = 89.6,
            OutsideAbsoluteHumidity = 21.2,

            WasHeating = false,
            WasCooling = true,
            LastHeatStarted = DateTimeOffset.Parse("2026-06-21T14:41:22-04:00"),
            LastHeatStopped = DateTimeOffset.Parse("2026-06-21T14:46:22-04:00"),
            LastCoolStarted = DateTimeOffset.Parse("2026-06-29T18:15:38-04:00"),
            LastCoolStopped = DateTimeOffset.Parse("2026-06-29T18:20:38-04:00")
        };

        var csv = HvacStatusCsvFormatter.Format(timestamp, message);

        Assert.Equal(
            "2026-06-29T18:32:10.0000000-04:00," +
            "2026-06-29T18:32:10.0000000-04:00," +
            "2026-06-29T18:32:00.0000000-04:00," +
            "2026-06-29T18:27:10.0000000-04:00," +
            "2026-06-29T18:30:10.0000000-04:00," +
            "false,true,true,Everything Normal,Cool," +
            "65.0,70.5,65.0,10.50," +
            "-4.5,3.0,-0.5,-2.0," +
            "69.8,56.5,10.35," +
            "63.9,63.1,9.53," +
            "10.35,89.6,21.20," +
            "false,true," +
            "2026-06-21T14:41:22.0000000-04:00," +
            "2026-06-21T14:46:22.0000000-04:00," +
            "2026-06-29T18:15:38.0000000-04:00," +
            "2026-06-29T18:20:38.0000000-04:00",
            csv);
    }

    [Fact]
    public void Format_LeavesOptionalValuesBlankWhenMissing()
    {
        var timestamp = DateTimeOffset.Parse("2026-06-29T18:32:10-04:00");

        var message = new ThermostatStatusMessage
        {
            Now = timestamp,
            Heat = false,
            Cool = false,
            Fan = false,
            Reason = "Everything Normal",
            Mode = HvacMode.Heat,
            HeatSetPointDay = 70.5,
            HeatSetPointNight = 65,
            MaxAbsHumSetPoint = 10.5,

            UpTempCalibration = 0,
            UpRelHumCalibration = 0,
            DownTempCalibration = 0,
            DownRelHumCalibration = 0
        };

        var csv = HvacStatusCsvFormatter.Format(timestamp, message);

        Assert.Equal(
            "2026-06-29T18:32:10.0000000-04:00," +
            "2026-06-29T18:32:10.0000000-04:00," +
            ",,," +
            "false,false,false,Everything Normal,Heat," +
            ",70.5,65.0,10.50," +
            "0.0,0.0,0.0,0.0," +
            ",,," +
            ",,," +
            ",,," +
            "false,false" +
            ",,,,",
            csv);
    }
}
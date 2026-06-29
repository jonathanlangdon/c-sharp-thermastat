using HvacController.Services;

namespace HvacController.Tests;

public sealed class MqttDataLoggerTests
{
    [Fact]
    public async Task HandleMessageAsync_LogsUpstairsSensorMessage()
    {
        var writer = new FakeCsvLogWriter();
        var logger = new MqttDataLogger(writer);

        var timestamp = new DateTimeOffset(
            2026, 6, 16, 14, 32, 10,
            TimeSpan.FromHours(-4));

        await logger.HandleMessageAsync(
            MqttDataLogger.UpstairsSensorTopic,
            "{\"temperatureFahr\":67.8,\"relativeHumidity\":62.6,\"motionDetected\":true,\"mode\":\"Cool\"}",
            timestamp,
            CancellationToken.None);

        var entry = Assert.Single(writer.Entries);

        Assert.Equal("upstairs-sensor.csv", entry.RelativePath);
        Assert.Equal(UpstairsSensorCsvFormatter.Header, entry.Header);
        Assert.Equal(
            "2026-06-16T14:32:10.0000000-04:00,67.8,62.6,true,Cool",
            entry.Line);
    }

    [Fact]
    public async Task HandleMessageAsync_LogsHvacStatusMessage()
    {
        var writer = new FakeCsvLogWriter();
        var logger = new MqttDataLogger(writer);

        var timestamp = new DateTimeOffset(
            2026, 6, 16, 14, 32, 10,
            TimeSpan.FromHours(-4));

        await logger.HandleMessageAsync(
            MqttDataLogger.HvacStatusTopic,
            "{" +
            "\"now\":\"2026-06-16T14:32:10-04:00\"," +
            "\"heat\":false," +
            "\"cool\":true," +
            "\"fan\":true," +
            "\"reason\":\"Manual test\"," +
            "\"mode\":\"Cool\"," +
            "\"heatSetPointFahr\":65," +
            "\"heatSetPointDay\":70.5," +
            "\"heatSetPointNight\":65," +
            "\"maxAbsHumSetPoint\":10.5," +
            "\"upTempCalibration\":-4.5," +
            "\"upRelHumCalibration\":3," +
            "\"downTempCalibration\":-0.5," +
            "\"downRelHumCalibration\":-2," +
            "\"upstairsTemperature\":76.1," +
            "\"upstairsRelativeHumidity\":55.2," +
            "\"upstairsAbsoluteHumidity\":11.19," +
            "\"downstairsTemperature\":72.0," +
            "\"downstairsRelativeHumidity\":50.1," +
            "\"downstairsAbsoluteHumidity\":9.3," +
            "\"controlAbsoluteHumidity\":11.19," +
            "\"outsideTemperature\":80.0," +
            "\"outsideAbsoluteHumidity\":9.23," +
            "\"wasHeating\":false," +
            "\"wasCooling\":true" +
            "}",
            timestamp,
            CancellationToken.None);

        var entry = Assert.Single(writer.Entries);

        Assert.Equal("hvac-status.csv", entry.RelativePath);
        Assert.Equal(HvacStatusCsvFormatter.Header, entry.Header);
        Assert.Equal(
            "2026-06-16T14:32:10.0000000-04:00," +
            "2026-06-16T14:32:10.0000000-04:00," +
            ",,," +
            "false,true,true,Manual test,Cool," +
            "65.0,70.5,65.0,10.50," +
            "-4.5,3.0,-0.5,-2.0," +
            "76.1,55.2,11.19," +
            "72.0,50.1,9.30," +
            "11.19,80.0,9.23," +
            "false,true" +
            ",,,,",
            entry.Line);
    }

    [Fact]
    public async Task HandleMessageAsync_IgnoresUnknownTopic()
    {
        var writer = new FakeCsvLogWriter();
        var logger = new MqttDataLogger(writer);

        await logger.HandleMessageAsync(
            "other/topic",
            "{}",
            DateTimeOffset.Now,
            CancellationToken.None);

        Assert.Empty(writer.Entries);
    }

    private sealed class FakeCsvLogWriter : ICsvLogWriter
    {
        public List<Entry> Entries { get; } = new();

        public Task AppendLineAsync(
            string relativePath,
            string header,
            string line,
            CancellationToken cancellationToken)
        {
            Entries.Add(new Entry(relativePath, header, line));
            return Task.CompletedTask;
        }
    }

    private sealed record Entry(
        string RelativePath,
        string Header,
        string Line);
}

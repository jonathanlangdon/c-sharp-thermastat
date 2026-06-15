using HvacController.Models;
using HvacController.Services;

namespace HvacController.Tests;

public sealed class DownstairsSensorUpdaterTests
{
    [Fact]
    public async Task RefreshAsync_WhenReaderReturnsReading_UpdatesDownstairsSensorState()
    {
        var now = DateTimeOffset.Parse("2026-06-12T12:00:00Z");

        var reader = new FakeDownstairsSensorReader(new DownstairsSensorReading
        {
            TemperatureFahr = 66.4,
            RelativeHumidity = 55.2,
            UpdatedAt = now
        });

        var state = new IndoorSensorState();
        var updater = new DownstairsSensorUpdater(reader, state);

        await updater.RefreshAsync(CancellationToken.None);

        Assert.Equal(66.4, state.CurrentTempFahrDown);
        Assert.Equal(55.2, state.HumidityDownstairs);
        Assert.Equal(now, state.LastSensorUpdate);
    }

    [Fact]
    public async Task RefreshAsync_WhenReaderReturnsNull_DoesNotUpdateState()
    {
        var reader = new FakeDownstairsSensorReader(null);
        var state = new IndoorSensorState();
        var updater = new DownstairsSensorUpdater(reader, state);

        await updater.RefreshAsync(CancellationToken.None);

        Assert.Null(state.CurrentTempFahrDown);
        Assert.Null(state.HumidityDownstairs);
        Assert.Null(state.LastSensorUpdate);
    }

    private sealed class FakeDownstairsSensorReader : IDownstairsSensorReader
    {
        private readonly DownstairsSensorReading? _reading;

        public FakeDownstairsSensorReader(DownstairsSensorReading? reading)
        {
            _reading = reading;
        }

        public Task<DownstairsSensorReading?> ReadAsync(
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_reading);
        }
    }
}

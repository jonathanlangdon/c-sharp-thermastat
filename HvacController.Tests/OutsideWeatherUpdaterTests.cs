using HvacController.Models;
using HvacController.Services;

namespace HvacController.Tests;

public sealed class OutsideWeatherUpdaterTests
{
    [Fact]
    public async Task RefreshAsync_WhenClientReturnsReading_UpdatesState()
    {
        var reading = new OutsideWeatherReading
        {
            OutsideTemperature = 68.0,
            OutsideAbsoluteHumidity = 8.64,
            UpdatedAt = DateTimeOffset.Parse("2026-06-12T12:00:00Z")
        };

        var client = new FakeOutsideWeatherClient(reading);
        var state = new OutsideWeatherState();
        var updater = new OutsideWeatherUpdater(client, state);

        await updater.RefreshAsync(CancellationToken.None);

        Assert.Equal(68.0, state.OutsideTemperature);
        Assert.Equal(8.64, state.OutsideAbsoluteHumidity);
        Assert.Equal(reading.UpdatedAt, state.LastUpdated);
    }

    [Fact]
    public async Task RefreshAsync_WhenClientReturnsNull_DoesNotUpdateState()
    {
        var client = new FakeOutsideWeatherClient(null);
        var state = new OutsideWeatherState();
        var updater = new OutsideWeatherUpdater(client, state);

        await updater.RefreshAsync(CancellationToken.None);

        Assert.Null(state.OutsideTemperature);
        Assert.Null(state.OutsideAbsoluteHumidity);
        Assert.Null(state.LastUpdated);
    }

    private sealed class FakeOutsideWeatherClient : IOutsideWeatherClient
    {
        private readonly OutsideWeatherReading? _reading;

        public FakeOutsideWeatherClient(OutsideWeatherReading? reading)
        {
            _reading = reading;
        }

        public Task<OutsideWeatherReading?> GetLatestAsync(
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_reading);
        }
    }
}
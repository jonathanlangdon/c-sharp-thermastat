using HvacController.Services;

namespace HvacController.Tests;

public sealed class OutsideWeatherBackgroundServiceTests
{
    [Fact]
    public async Task StartAsync_RefreshesWeatherImmediately()
    {
        var updater = new FakeOutsideWeatherUpdater();
        var service = new OutsideWeatherBackgroundService(
            updater,
            TimeSpan.FromMilliseconds(50));

        await service.StartAsync(CancellationToken.None);

        await updater.WaitForRefreshAsync(TimeSpan.FromSeconds(1));

        await service.StopAsync(CancellationToken.None);

        Assert.True(updater.RefreshCount >= 1);
    }

    private sealed class FakeOutsideWeatherUpdater : IOutsideWeatherUpdater
    {
        private readonly TaskCompletionSource _firstRefresh = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        private int _refreshCount;

        public int RefreshCount => Volatile.Read(ref _refreshCount);

        public Task RefreshAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _refreshCount);
            _firstRefresh.TrySetResult();

            return Task.CompletedTask;
        }

        public async Task WaitForRefreshAsync(TimeSpan timeout)
        {
            var completedTask = await Task.WhenAny(
                _firstRefresh.Task,
                Task.Delay(timeout));

            Assert.Same(_firstRefresh.Task, completedTask);
        }
    }
}
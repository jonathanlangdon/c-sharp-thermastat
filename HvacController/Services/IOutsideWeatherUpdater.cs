namespace HvacController.Services;

public interface IOutsideWeatherUpdater
{
    Task RefreshAsync(CancellationToken cancellationToken);
}
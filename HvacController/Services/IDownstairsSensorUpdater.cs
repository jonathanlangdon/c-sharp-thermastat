namespace HvacController.Services;

public interface IDownstairsSensorUpdater
{
    Task RefreshAsync(CancellationToken cancellationToken);
}

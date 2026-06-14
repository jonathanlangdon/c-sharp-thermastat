namespace HvacController.Services;

public sealed class OutsideWeatherUpdater
{
    private readonly IOutsideWeatherClient _client;
    private readonly OutsideWeatherState _state;

    public OutsideWeatherUpdater(
        IOutsideWeatherClient client,
        OutsideWeatherState state)
    {
        _client = client;
        _state = state;
    }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var reading = await _client.GetLatestAsync(cancellationToken);

        if (reading is null)
        {
            return;
        }

        _state.Update(reading);
    }
}
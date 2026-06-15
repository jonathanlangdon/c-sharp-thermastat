namespace HvacController.Services;

public sealed class DownstairsSensorUpdater : IDownstairsSensorUpdater
{
    private readonly IDownstairsSensorReader _reader;
    private readonly IndoorSensorState _state;

    public DownstairsSensorUpdater(
        IDownstairsSensorReader reader,
        IndoorSensorState state)
    {
        _reader = reader;
        _state = state;
    }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var reading = await _reader.ReadAsync(cancellationToken);

        if (reading is null)
        {
            return;
        }

        _state.UpdateDownstairsSensor(
            temperatureFahr: reading.TemperatureFahr,
            relativeHumidity: reading.RelativeHumidity,
            updatedAt: reading.UpdatedAt);
    }
}

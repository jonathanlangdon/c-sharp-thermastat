namespace HvacController.Services;

public sealed class UpstairsSensorMessageHandler
{
    private readonly IndoorSensorState _state;

    public UpstairsSensorMessageHandler(IndoorSensorState state)
    {
        _state = state;
    }

    public bool Handle(string json, DateTimeOffset receivedAt)
    {
        var message = UpstairsSensorMessageParser.Parse(json);

        if (message is null)
        {
            return false;
        }

        _state.UpdateUpstairsSensor(
            temperatureFahr: message.TemperatureFahr,
            relativeHumidity: message.RelativeHumidity,
            updatedAt: receivedAt);

        if (message.MotionDetected == true)
        {
            _state.RecordMotion(receivedAt);
        }

        if (message.Mode is not null)
        {
            _state.SetMode(message.Mode.Value);
        }

        return true;
    }
}

using HvacController.Models;

namespace HvacController.Services;

public sealed class OutsideWeatherState
{
    private readonly object _lock = new();

    private OutsideWeatherReading? _latestReading;

    public double? OutsideTemperature
    {
        get
        {
            lock (_lock)
            {
                return _latestReading?.OutsideTemperature;
            }
        }
    }

    public double? OutsideAbsoluteHumidity
    {
        get
        {
            lock (_lock)
            {
                return _latestReading?.OutsideAbsoluteHumidity;
            }
        }
    }

    public DateTimeOffset? LastUpdated
    {
        get
        {
            lock (_lock)
            {
                return _latestReading?.UpdatedAt;
            }
        }
    }

    public void Update(OutsideWeatherReading reading)
    {
        lock (_lock)
        {
            _latestReading = reading;
        }
    }
}
using HvacController.Models;

namespace HvacController.Services;

public sealed class IndoorSensorState
{
    private readonly object _lock = new();

    private double? _currentTempFahrUp;
    private double? _currentTempFahrDown;
    private double? _humidityUpstairs;
    private double? _humidityDownstairs;
    private DateTimeOffset? _lastSensorUpdate;
    private DateTimeOffset? _lastMotionDetected;
    private HvacMode _mode = HvacMode.Heat;

    public double? CurrentTempFahrUp
    {
        get
        {
            lock (_lock)
            {
                return _currentTempFahrUp;
            }
        }
    }

    public double? CurrentTempFahrDown
    {
        get
        {
            lock (_lock)
            {
                return _currentTempFahrDown;
            }
        }
    }

    public double? HumidityUpstairs
    {
        get
        {
            lock (_lock)
            {
                return _humidityUpstairs;
            }
        }
    }

    public double? HumidityDownstairs
    {
        get
        {
            lock (_lock)
            {
                return _humidityDownstairs;
            }
        }
    }

    public DateTimeOffset? LastSensorUpdate
    {
        get
        {
            lock (_lock)
            {
                return _lastSensorUpdate;
            }
        }
    }

    public DateTimeOffset? LastMotionDetected
    {
        get
        {
            lock (_lock)
            {
                return _lastMotionDetected;
            }
        }
    }

    public HvacMode Mode
    {
        get
        {
            lock (_lock)
            {
                return _mode;
            }
        }
    }

    public void UpdateUpstairsSensor(
        double temperatureFahr,
        double relativeHumidity,
        DateTimeOffset updatedAt)
    {
        lock (_lock)
        {
            _currentTempFahrUp = temperatureFahr;
            _humidityUpstairs = relativeHumidity;
            _lastSensorUpdate = updatedAt;
        }
    }

    public void UpdateDownstairsSensor(
        double temperatureFahr,
        double relativeHumidity,
        DateTimeOffset updatedAt)
    {
        lock (_lock)
        {
            _currentTempFahrDown = temperatureFahr;
            _humidityDownstairs = relativeHumidity;
            _lastSensorUpdate = updatedAt;
        }
    }

    public void RecordMotion(DateTimeOffset detectedAt)
    {
        lock (_lock)
        {
            _lastMotionDetected = detectedAt;
        }
    }

    public void SetMode(HvacMode mode)
    {
        lock (_lock)
        {
            _mode = mode;
        }
    }
}
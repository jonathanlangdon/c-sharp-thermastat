using System.Device.I2c;
using HvacController.Models;
using Iot.Device.Sht4x;

namespace HvacController.Services;

public sealed class Sht45DownstairsSensorReader : IDownstairsSensorReader, IDisposable
{
    private I2cDevice? _device;
    private Sht4x? _sensor;

    public async Task<DownstairsSensorReading?> ReadAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            EnsureSensorInitialized();

            if (_sensor is null)
            {
                return null;
            }

            var (relativeHumidity, temperature) =
                await _sensor.ReadHumidityAndTemperatureAsync();

            if (relativeHumidity is null || temperature is null)
            {
                return null;
            }

            var temperatureFahr =
                temperature.Value.DegreesCelsius * 9 / 5 + 32;

            return new DownstairsSensorReading
            {
                TemperatureFahr = Math.Round(temperatureFahr, 1),
                RelativeHumidity = Math.Round(relativeHumidity.Value.Percent, 1),
                UpdatedAt = DateTimeOffset.Now
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            DisposeSensor();

            return null;
        }
    }

    private void EnsureSensorInitialized()
    {
        if (_sensor is not null)
        {
            return;
        }

        I2cDevice? device = null;

        try
        {
            var settings = new I2cConnectionSettings(
                busId: 1,
                deviceAddress: Sht4x.DefaultI2cAddress);

            device = I2cDevice.Create(settings);
            var sensor = new Sht4x(device);

            _device = device;
            _sensor = sensor;
        }
        catch
        {
            device?.Dispose();

            throw;
        }
    }

    private void DisposeSensor()
    {
        _sensor?.Dispose();
        _device?.Dispose();

        _sensor = null;
        _device = null;
    }

    public void Dispose()
    {
        DisposeSensor();
    }
}
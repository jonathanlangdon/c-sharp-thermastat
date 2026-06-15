using System.Device.I2c;
using HvacController.Models;
using Iot.Device.Sht4x;

namespace HvacController.Services;

public sealed class Sht45DownstairsSensorReader : IDownstairsSensorReader, IDisposable
{
    private readonly I2cDevice _device;
    private readonly Sht4x _sensor;

    public Sht45DownstairsSensorReader()
    {
        var settings = new I2cConnectionSettings(
            busId: 1,
            deviceAddress: Sht4x.DefaultI2cAddress);

        _device = I2cDevice.Create(settings);
        _sensor = new Sht4x(_device);
    }

    public async Task<DownstairsSensorReading?> ReadAsync(
        CancellationToken cancellationToken)
    {
        try
        {
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
            return null;
        }
    }

    public void Dispose()
    {
        _sensor.Dispose();
        _device.Dispose();
    }
}

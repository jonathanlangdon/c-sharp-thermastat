using System.Device.Gpio;

namespace HvacController.Services;

public sealed class RelayService : IRelayService, IDisposable
{
    private readonly GpioController _gpio;
    private readonly bool _activeHigh;

    private const int HeatPin = 17;  // physical pin 11 -> IN1
    private const int CoolPin = 27;  // physical pin 13 -> IN2
    private const int FanPin = 22;   // physical pin 15 -> IN3
    private const int SparePin = 23; // physical pin 16 -> IN4

    public RelayService(bool activeHigh)
    {
        _activeHigh = activeHigh;
        _gpio = new GpioController();

        OpenOutput(HeatPin);
        OpenOutput(CoolPin);
        OpenOutput(FanPin);
        OpenOutput(SparePin);

        AllOff();
    }

    private void OpenOutput(int pin)
    {
        if (!_gpio.IsPinOpen(pin))
        {
            _gpio.OpenPin(pin, PinMode.Output);
        }

        WriteOff(pin);
    }

    private void WriteOn(int pin)
    {
        _gpio.Write(pin, _activeHigh ? PinValue.High : PinValue.Low);
    }

    private void WriteOff(int pin)
    {
        _gpio.Write(pin, _activeHigh ? PinValue.Low : PinValue.High);
    }

    public void AllOff()
    {
        WriteOff(HeatPin);
        WriteOff(CoolPin);
        WriteOff(FanPin);
        WriteOff(SparePin);
    }

    public void SetRelays(bool heat, bool cool, bool fan)
    {
        // Hard safety rule: never heat and cool together.
        if (heat && cool)
        {
            heat = false;
            cool = false;
            fan = false;
        }

        WriteOff(HeatPin);
        WriteOff(CoolPin);
        WriteOff(FanPin);
        WriteOff(SparePin);

        if (heat) WriteOn(HeatPin);
        if (cool) WriteOn(CoolPin);
        if (fan) WriteOn(FanPin);
    }

    public void Dispose()
    {
        AllOff();
        _gpio.Dispose();
    }
}

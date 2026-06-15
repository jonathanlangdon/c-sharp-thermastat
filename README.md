# C# Thermostat

A C#/.NET HVAC thermostat controller that heats based on indoor temperature and cools based on absolute humidity.

The controller runs on a Raspberry Pi, receives upstairs sensor data over MQTT from an ESP32 display, reads outside weather from weather.gov, and controls HVAC relays through GPIO.

## Current Control Logic

### Heat

* Heat mode only.
* Uses upstairs temperature as the control temperature.
* Day heat set point: `70.6°F`
* Night heat set point: `65.0°F`
* Night schedule: `7:00 PM` through `6:00 AM`
* Day heat set point is only used if motion has been detected in the past 2 hours.
* If there has not been recent motion, heat defaults to the night set point.

### Cooling

* Cool mode only.
* Cooling is based on maximum indoor absolute humidity between upstairs and downstairs.
* Cooling turns on at or above `9.5 g/m³`.
* Cooling turns off below `9.0 g/m³`.
* If outside absolute humidity is below `9.0 g/m³`, AC is locked out.
* If outside humidity data is unavailable, cooling is allowed.

### Fan

* Fan runs during normal operation.
* Fan turns off during safety/error states.

### Safety Rules

* Heat and cool are never energized at the same time.
* Minimum run time: 5 minutes.
* Minimum off time: 5 minutes.
* Sensor timeout: 3 minutes.
* Missing temperature, missing humidity, or stale sensor data turns all relays off.

## Hardware

* Raspberry Pi 4 Model B, 4GB
* SHT45 high-accuracy temperature/humidity module, x2
* ELEGOO 4-channel DC 5V relay module with optocoupler
* Qoroos LD2410C human presence radar sensor
* Waveshare ESP32-S3 3.5-inch capacitive touch display, Type B, 320x480 IPS panel

## GPIO Relay Mapping

| HVAC Function |   GPIO | Physical Pin | Relay Input |
| ------------- | -----: | -----------: | ----------- |
| Heat          | GPIO17 |       Pin 11 | IN1         |
| Cool          | GPIO27 |       Pin 13 | IN2         |
| Fan           | GPIO22 |       Pin 15 | IN3         |
| Spare         | GPIO23 |       Pin 16 | IN4         |

The relay board is active-low:

| GPIO Value | Relay State |
| ---------: | ----------- |
|        `1` | Off         |
|        `0` | On          |

## Testing Relays from Terminal

Turn all relays off:

```bash
gpioset --chip gpiochip0 17=1 27=1 22=1 23=1
```

Turn GPIO17 relay on:

```bash
gpioset --chip gpiochip0 17=0
```

## Run Test Suite

From the repository root:

```bash
dotnet test
```

## Manual Run

From the app project directory:

```bash
cd HvacController
dotnet run
```

## System Service

Check service status:

```bash
systemctl status hvac-controller
```

Follow logs from the current boot:

```bash
journalctl -u hvac-controller -b -f
```

Start the service:

```bash
sudo systemctl start hvac-controller
```

Stop the service:

```bash
sudo systemctl stop hvac-controller
```

Enable service startup on boot:

```bash
sudo systemctl enable hvac-controller
```

## MQTT

Mosquitto is used as the local MQTT broker.

### Upstairs ESP32 MQTT Topic

```text
hvac/upstairs/sensor
```

### Upstairs ESP32 MQTT JSON Format

```json
{
  "temperatureFahr": 72.1,
  "relativeHumidity": 48.5,
  "motionDetected": true,
  "mode": "Cool"
}
```

Valid modes:

```text
Heat
Cool
```

### Publish a Test MQTT Message

```bash
mosquitto_pub -h localhost -t hvac/upstairs/sensor -m '{
  "temperatureFahr": 72.1,
  "relativeHumidity": 50,
  "motionDetected": true,
  "mode": "Cool"
}'
```

## Outside Weather Source

Outside weather is read from the National Weather Service latest observations endpoint for station `KMKG`:

```text
https://api.weather.gov/stations/KMKG/observations/latest
```

The app stores:

* `OutsideTemperature`
* `OutsideAbsoluteHumidity`

## Development vs Live Relay Mode

In `Program.cs`, development mode uses the no-op relay service so the app can run without GPIO access:

```csharp
builder.Services.AddSingleton<IRelayService, NoOpRelayService>();
```

Live relay mode uses the real GPIO relay service:

```csharp
builder.Services.AddSingleton<IRelayService>(_ => new RelayService(activeHigh: false));
```

Only enable live relay mode when running on the Raspberry Pi with the relay board connected.


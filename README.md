# C# Thermostat

A C#/.NET HVAC thermostat controller that heats based on indoor temperature and cools based on indoor absolute humidity.

The controller runs on a Raspberry Pi, receives upstairs sensor data over MQTT from a Waveshare ESP32-S3 touch display, reads outside weather from weather.gov, and controls HVAC relays through GPIO.

The Waveshare display provides:

* Main thermostat screen
* Settings screen
* Editable humidity targets
* Editable day/night heat set points
* Manual override and manual mode controls
* MQTT publishing back to the controller for persistent settings

## Current Control Logic

### Heat

* Heat mode only.
* Uses upstairs temperature as the control temperature.
* Day heat set point default: `70.5°F`
* Night heat set point default: `65.0°F`
* Night schedule: `7:00 PM` through `6:00 AM`
* During the day, the day heat set point is only used if motion has been detected recently.
* If there has not been recent motion, heat uses the night set point.
* Motion hold time default: `1 hour`

### Cooling

* Cool mode only.
* Cooling is based on the maximum indoor absolute humidity between upstairs and downstairs.
* The active cooling humidity set point is calculated from upstairs temperature:

| Upstairs Temperature | Active Target |
| -------------------: | ------------- |
| `>= 72°F`            | Ideal humidity target |
| `>= 71°F` and `<72°F` | Good humidity target |
| `< 71°F`             | Fair humidity target |

Default humidity targets:

| Target | Default |
| ------ | ------: |
| Ideal  | `9.0 g/m³` |
| Good   | `10.0 g/m³` |
| Fair   | `11.0 g/m³` |

During the high-demand pricing window, cooling uses the fair humidity target regardless of upstairs temperature.

High-demand pricing window:

```text
June 1 through September 30
Weekdays only
2:00 PM through 7:00 PM
```

If outside humidity data is unavailable, cooling is disabled and the fan remains on.

### Fan

* Fan runs during normal heat/cool operation.
* Fan remains on in normal idle states.
* Fan turns off during safety/error states.

### Safety Rules

* Heat and cool are never energized at the same time.
* Minimum run time: `5 minutes`
* Minimum off time: `5 minutes`
* Sensor timeout: `3 minutes`
* Missing temperature, missing humidity, or stale sensor data turns all relays off.

## Persistent State

User-editable settings are stored in the persistent state file on the Raspberry Pi.

Typical path:

```text
/home/langdon/hvac-controller/HvacController/state/thermostat-state.json
```

Persistent values include:

* HVAC mode
* Day heat set point
* Night heat set point
* Ideal humidity target
* Good humidity target
* Fair humidity target
* Manual override
* Manual mode
* Temperature/humidity calibration values
* Last sensor update
* Last motion detected
* Runtime totals
* Last heat/cool start and stop timestamps

`maxAbsHumSetPoint` is not a stored setting. It is the currently active calculated humidity target published in MQTT status.

## Manual Override

Manual override values are currently stored and published through MQTT/status.

Current manual fields:

| Field | Values |
| ----- | ------ |
| `manualOverride` | `true` / `false` |
| `manualMode` | `Off` / `Fan` |

The Waveshare settings screen can toggle these values and publish them back to the controller through MQTT.

## Hardware

* Raspberry Pi 4 Model B, 4GB
* SHT45 high-accuracy temperature/humidity module, x2
* ELEGOO 4-channel DC 5V relay module with optocoupler
* Qoroos LD2410C human presence radar sensor
* Waveshare ESP32-S3 3.5-inch capacitive touch display, Type B, 320x480 IPS panel

## GPIO Pinout

| HVAC Function | GPIO   | Physical Pin | Relay Input |
| ------------- | -----: | -----------: | ----------- |
| Heat          | GPIO17 | Pin 11       | IN1         |
| Cool          | GPIO27 | Pin 13       | IN2         |
| Fan           | GPIO22 | Pin 15       | IN3         |
| Spare         | GPIO23 | Pin 16       | IN4         |

The relay board is active-low:

| GPIO Value | Relay State |
| ---------: | ----------- |
| `1`        | Off         |
| `0`        | On          |

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

Restart the service:

```bash
sudo systemctl restart hvac-controller
```

Enable service startup on boot:

```bash
sudo systemctl enable hvac-controller
```

## MQTT

Mosquitto is used as the local MQTT broker.

### Topics

| Topic | Direction | Purpose |
| ----- | --------- | ------- |
| `hvac/upstairs/sensor` | Waveshare -> Controller | Upstairs temp/RH/motion |
| `hvac/status` | Controller -> Displays/loggers | Full HVAC status |
| `hvac/mode/set` | Waveshare -> Controller | Change Heat/Cool mode |
| `hvac/settings/set` | Waveshare -> Controller | Save settings screen values |
| `hvac/upstairs/boot` | Waveshare -> Broker | Boot notification |

### Upstairs Sensor JSON

Topic:

```text
hvac/upstairs/sensor
```

Example:

```json
{
  "temperatureFahr": 72.1,
  "relativeHumidity": 48.5,
  "motionDetected": true
}
```

### Mode Set JSON

Topic:

```text
hvac/mode/set
```

Example:

```json
{
  "mode": "Cool"
}
```

Valid modes:

```text
Heat
Cool
```

### Settings Set JSON

Topic:

```text
hvac/settings/set
```

Example:

```json
{
  "heatSetPointDay": 70.5,
  "heatSetPointNight": 65.0,
  "humidityTargetFair": 11.0,
  "humidityTargetGood": 10.0,
  "humidityTargetIdeal": 9.0,
  "manualOverride": false,
  "manualMode": "Off"
}
```

Valid manual modes:

```text
Off
Fan
```

### HVAC Status JSON

Topic:

```text
hvac/status
```

Example fields:

```json
{
  "now": "2026-07-27T14:37:18",
  "lastSensorUpdate": "2026-07-27T14:37:01",
  "lastMotionDetected": "2026-07-27T14:36:01",
  "outsideWeatherUpdatedAt": "2026-07-27T14:34:37",

  "heat": false,
  "cool": false,
  "fan": true,
  "reason": "Everything Normal",
  "mode": "Cool",

  "heatSetPointFahr": 70.5,
  "heatSetPointDay": 70.5,
  "heatSetPointNight": 65.0,

  "humidityTargetIdeal": 9.0,
  "humidityTargetGood": 10.0,
  "humidityTargetFair": 11.0,
  "maxAbsHumSetPoint": 11.0,

  "manualOverride": false,
  "manualMode": "Off",

  "upTempCalibration": -4.5,
  "upRelHumCalibration": 3.0,
  "downTempCalibration": -0.5,
  "downRelHumCalibration": -1.5,

  "upstairsTemperature": 64.9,
  "upstairsRelativeHumidity": 62.7,
  "upstairsAbsoluteHumidity": 9.79,

  "downstairsTemperature": 68.8,
  "downstairsRelativeHumidity": 58.0,
  "downstairsAbsoluteHumidity": 10.29,

  "controlAbsoluteHumidity": 10.29,

  "outsideTemperature": 73.4,
  "outsideAbsoluteHumidity": 19.34,

  "wasHeating": false,
  "wasCooling": false,
  "coolHoursToday": 3.33,
  "heatHoursToday": 0.0,

  "lastHeatStarted": "2026-07-24T13:27:40",
  "lastHeatStopped": "2026-07-24T13:27:50",
  "lastCoolStarted": "2026-07-27T12:02:44",
  "lastCoolStopped": "2026-07-27T12:09:44"
}
```

## Publish a Test MQTT Message in Development Mode

Terminal 1:

```bash
dotnet run
```

Terminal 2:

```bash
mosquitto_sub -h localhost -t hvac/status -v
```

Terminal 3:

```bash
mosquitto_pub -h localhost -t hvac/upstairs/sensor -m '{
  "temperatureFahr": 66.1,
  "relativeHumidity": 50,
  "motionDetected": true
}'
```

Publish a mode change:

```bash
mosquitto_pub -h localhost -t hvac/mode/set -m '{
  "mode": "Cool"
}'
```

Publish settings:

```bash
mosquitto_pub -h localhost -t hvac/settings/set -m '{
  "heatSetPointDay": 70.5,
  "heatSetPointNight": 65.0,
  "humidityTargetFair": 11.0,
  "humidityTargetGood": 10.0,
  "humidityTargetIdeal": 9.0,
  "manualOverride": false,
  "manualMode": "Off"
}'
```

## View Live MQTT Messages

One status message:

```bash
mosquitto_sub -h localhost -t hvac/status -C 1 -v
```

Upstairs sensor messages:

```bash
mosquitto_sub -h localhost -t hvac/upstairs/sensor -v
```

Formatted status from the Pi:

```bash
mosquitto_sub -h hvac.local -t hvac/status -F '%I %p' | while read -r ts json; do
    echo "$ts"
    echo "$json" | jq -M .
    echo
done
```

Formatted upstairs sensor messages:

```bash
mosquitto_sub -h hvac.local -t hvac/upstairs/sensor -F '%I %p' | while read -r ts json; do
    echo "$ts"
    echo "$json" | jq -M .
    echo
done
```

## Outside Weather Source

Outside weather is read from the National Weather Service latest observations endpoint for station `KMKG`:

```text
https://api.weather.gov/stations/KMKG/observations/latest
```

The app stores:

* Outside temperature
* Outside absolute humidity
* Outside weather updated timestamp

## Runtime Logging

The controller tracks daily runtime totals:

* `coolHoursToday`
* `heatHoursToday`

These values are published in `hvac/status`.

A daily runtime CSV can be written from the persistent state file with a cron job at `11:59 PM`.

Example CSV fields:

```text
year,month,day,dayOfWeek,coolHoursToday,heatHoursToday
```

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

## Arduino Settings for Waveshare

Use these Arduino IDE settings for the Waveshare ESP32-S3 display:

```text
Board: ESP32S3 Dev Module
USB CDC On Boot: Enabled
USB Mode: Hardware CDC and JTAG
Upload Mode: UART0 / Hardware CDC
Flash Size: 16MB
Partition Scheme: 3MB APP / 9MB FATFS
PSRAM: OPI PSRAM
Flash Mode: QIO 80MHz
CPU Frequency: 240MHz
Upload Speed: 460800
```

## Waveshare Display

The Waveshare ESP32-S3 display provides:

* Main thermostat screen
* Gear/settings button
* Settings screen
* Editable humidity targets
* Editable day/night heat set points
* Manual override toggle
* Manual mode toggle between `Off` and `Fan`
* Draft editing while on the settings screen
* Settings are published to `hvac/settings/set` when exiting the settings screen

The settings screen uses a local draft copy of MQTT values while editing. Incoming MQTT status messages do not overwrite the values being edited until the settings screen is exited and opened again.

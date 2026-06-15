# c-sharp-thermastat
Thermastat using C# for code that cools based on humidity and heats based on temp

## Hardware
- Raspberry Pi 4 Model B 2019 Quad Core 64 Bit WiFi Bluetooth (4GB) 
- Sht45 High Accuracy Temperature Humidity Module Low Power I2c (x2)
- ELEGOO 4 Channel DC 5V Relay Module with Optocoupler
- Qoroos LD2410C Sensor Module HLK-LD2410 Human Presence Radar LD2410 Millimeter Wave Radar Sensor Module Non Contact 24GHz ISM Band Serial Port IO Level Output
- Waveshare ESP32-S3 3.5inch Capacitive Touch Display Development Board Type B, 320×480 Pixels, IPS Panel

##Testing relay from Terminal
gpioset --chip gpiochip0 17=1 27=1 22=1 23=1
-> all off
gpioset --chip gpiochip0 17=0
-> gpi 17 on

##Run Test suite
dotnet test

##Manual Run Program
dotnet run

##Source of outside weather
https://api.weather.gov/stations/KMKG/observations/latest


##upstairs ESP32 MQTT JSON Format
{
  "temperatureFahr": 72.1,
  "relativeHumidity": 48.5,
  "motionDetected": true,
  "mode": "Cool"
}

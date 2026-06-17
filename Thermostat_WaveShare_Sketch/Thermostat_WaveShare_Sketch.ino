#include <Arduino_GFX_Library.h>
#include <Adafruit_GFX.h>
#include <WiFi.h>
#include <PubSubClient.h>
#include <ArduinoJson.h>
#include "TCA9554.h"
#include "secrets.h"
#include <Adafruit_SHT4x.h>
#include "esp_lcd_touch_axs15231b.h"

#include <Fonts/FreeSans9pt7b.h>
#include <Fonts/FreeSans12pt7b.h>
#include <Fonts/FreeSansBold18pt7b.h>
#include <Fonts/FreeSansBold24pt7b.h>

#define LCD_QSPI_CS   12
#define LCD_QSPI_CLK  5
#define LCD_QSPI_D0   1
#define LCD_QSPI_D1   2
#define LCD_QSPI_D2   3
#define LCD_QSPI_D3   4

#define ROTATION 1
#define GFX_BL 6

#define I2C_SDA 8
#define I2C_SCL 7

#define SHT45_SDA I2C_SDA
#define SHT45_SCL I2C_SCL

#define SCREEN_W 480
#define SCREEN_H 320

#define SAFE_X 45
#define SAFE_Y 55
#define SAFE_RIGHT_MARGIN 35
#define SAFE_BOTTOM_MARGIN 40

#define SAFE_W (SCREEN_W - SAFE_X - SAFE_RIGHT_MARGIN)
#define SAFE_H (SCREEN_H - SAFE_Y - SAFE_BOTTOM_MARGIN)

#define LD2410_OUT_PIN 43

#define BOTTOM_BAR_H 42

#define MODE_SMALL_W 90
#define MODE_ANIMATION_STEPS 14
#define MODE_ANIMATION_DELAY_MS 16

int bottomBarHeatW = SAFE_W - MODE_SMALL_W;


bool touchReady = false;
unsigned long lastTouchLogMs = 0;

bool motionDetected = false;

Adafruit_SHT4x sht45 = Adafruit_SHT4x();

bool sht45Ready = false;
double upstairsTemperatureF = NAN;
double upstairsRelativeHumidity = NAN;

String selectedMode = "Cool";

const char* MQTT_HOST = "hvac.local";
const int MQTT_PORT = 1883;

const char* SENSOR_TOPIC = "hvac/upstairs/sensor";
const char* STATUS_TOPIC = "hvac/status";

TCA9554 TCA(0x20);

Arduino_DataBus *bus = new Arduino_ESP32QSPI(
  LCD_QSPI_CS,
  LCD_QSPI_CLK,
  LCD_QSPI_D0,
  LCD_QSPI_D1,
  LCD_QSPI_D2,
  LCD_QSPI_D3
);

Arduino_GFX *g = new Arduino_AXS15231B(
  bus,
  -1,
  0,
  false,
  320,
  480
);

Arduino_Canvas *gfx = new Arduino_Canvas(
  320,
  480,
  g,
  0,
  0,
  ROTATION
);

WiFiClient wifiClient;
PubSubClient mqttClient(wifiClient);


#define HVAC_BG         RGB565_BLACK
#define HVAC_TEXT       RGB565_WHITE
#define HVAC_LINE       0x7BEF
#define HVAC_HEAT_FILL  0xF800
#define HVAC_COOL_FILL  0x001F

struct HvacStatus
{
  bool heat = false;
  bool cool = false;
  bool fan = false;

  String reason = "Waiting";
  String mode = "Heat";

  double heatSetPointFahr = NAN;

  double upstairsTemperature = NAN;
  double upstairsAbsoluteHumidity = NAN;
  double downstairsAbsoluteHumidity = NAN;
  double outsideTemperature = NAN;
  double outsideAbsoluteHumidity = NAN;
};

HvacStatus latestStatus;

unsigned long lastPublishMs = 0;
const unsigned long publishIntervalMs = 30000;


void initTouch()
{
  Serial.println("Initializing AXS15231B touch...");

  bsp_touch_init(&Wire, -1, 0, 320, 480);

  touchReady = true;

  Serial.println("AXS15231B touch initialized.");
}


void initDisplay()
{
  Wire.begin(I2C_SDA, I2C_SCL);
  Wire.setTimeOut(100);

  TCA.begin();
  TCA.pinMode1(1, OUTPUT);

  TCA.write1(1, 1);
  delay(10);
  TCA.write1(1, 0);
  delay(10);
  TCA.write1(1, 1);
  delay(200);

  initTouch();

  if (!gfx->begin())
  {
    Serial.println("gfx->begin() failed!");
  }

  pinMode(GFX_BL, OUTPUT);
  digitalWrite(GFX_BL, HIGH);
}


void setFontSmall(uint16_t color)
{
  gfx->setFont(&FreeSans9pt7b);
  gfx->setTextSize(1);
  gfx->setTextColor(color);
}

void setFontMedium(uint16_t color)
{
  gfx->setFont(&FreeSans12pt7b);
  gfx->setTextSize(1);
  gfx->setTextColor(color);
}

void setFontHeading(uint16_t color)
{
  gfx->setFont(&FreeSansBold18pt7b);
  gfx->setTextSize(1);
  gfx->setTextColor(color);
}

void printAt(int x, int y, const char* text)
{
  gfx->setCursor(x, y);
  gfx->print(text);
}

void printAtString(int x, int y, const String& text)
{
  gfx->setCursor(x, y);
  gfx->print(text);
}

String formatSetPoint(double value)
{
  if (isnan(value))
  {
    return "--";
  }

  return String((int)floor(value));
}

String formatOneDecimal(double value)
{
  if (isnan(value))
  {
    return "--";
  }

  return String(value, 1);
}

void getBounds(
  const char* text,
  const GFXfont* font,
  int size,
  int16_t* x1,
  int16_t* y1,
  uint16_t* w,
  uint16_t* h)
{
  gfx->setFont(font);
  gfx->setTextSize(size);
  gfx->getTextBounds(text, 0, 0, x1, y1, w, h);
}

void drawCenteredTextInBox(
  int boxX,
  int boxY,
  int boxW,
  int boxH,
  const char* text,
  uint16_t color)
{
  int16_t x1;
  int16_t y1;
  uint16_t textW;
  uint16_t textH;

  gfx->getTextBounds(text, 0, 0, &x1, &y1, &textW, &textH);

  int textX = boxX + ((boxW - textW) / 2) - x1;
  int textY = boxY + ((boxH - textH) / 2) - y1;

  gfx->setCursor(textX, textY);
  gfx->setTextColor(color);
  gfx->print(text);
}

void drawTopBar()
{
  setFontMedium(HVAC_TEXT);

  String outside = "Outside: ";
  outside += formatOneDecimal(latestStatus.outsideTemperature);

  printAtString(SAFE_X, SAFE_Y + 20, outside);
}

void drawLargeTemperature()
{
  const int areaX = SAFE_X;
  const int areaY = SAFE_Y + 40;
  const int areaW = 245;
  const int areaH = 125;

  String temp = formatOneDecimal(latestStatus.upstairsTemperature);

  String wholePart = "--";
  String decimalPart = "";

  int dotIndex = temp.indexOf('.');

  if (dotIndex >= 0)
  {
    wholePart = temp.substring(0, dotIndex);
    decimalPart = temp.substring(dotIndex);
  }
  else
  {
    wholePart = temp;
  }

  int16_t bigX1;
  int16_t bigY1;
  uint16_t bigW;
  uint16_t bigH;

  int16_t smallX1;
  int16_t smallY1;
  uint16_t smallW;
  uint16_t smallH;

  getBounds(wholePart.c_str(), &FreeSansBold24pt7b, 2, &bigX1, &bigY1, &bigW, &bigH);
  getBounds(decimalPart.c_str(), &FreeSansBold24pt7b, 1, &smallX1, &smallY1, &smallW, &smallH);

  int gap = 6;
  int totalW = bigW + gap + smallW;

  int startX = areaX + ((areaW - totalW) / 2);
  int bigBaselineY = areaY + ((areaH - bigH) / 2) - bigY1;

  gfx->setFont(&FreeSansBold24pt7b);
  gfx->setTextSize(2);
  gfx->setTextColor(HVAC_TEXT);
  gfx->setCursor(startX - bigX1, bigBaselineY);
  gfx->print(wholePart);

  int smallX = startX + bigW + gap;

  gfx->setFont(&FreeSansBold24pt7b);
  gfx->setTextSize(1);
  gfx->setTextColor(HVAC_TEXT);
  gfx->setCursor(smallX - smallX1, bigBaselineY);
  gfx->print(decimalPart);
}

void drawHumidityPanel()
{
  int x = SAFE_X + 268;
  int y = SAFE_Y + 48;

  setFontMedium(HVAC_TEXT);
  printAt(x, y, "Humidity");

  setFontSmall(HVAC_TEXT);

  String out = "Out: ";
  out += formatOneDecimal(latestStatus.outsideAbsoluteHumidity);
  printAtString(x, y + 34, out);

  String up = "Up: ";
  up += formatOneDecimal(latestStatus.upstairsAbsoluteHumidity);
  printAtString(x, y + 62, up);

  String down = "Down: ";
  down += formatOneDecimal(latestStatus.downstairsAbsoluteHumidity);
  printAtString(x, y + 90, down);
}

int getTargetHeatWidth()
{
  if (selectedMode == "Cool")
  {
    return MODE_SMALL_W;
  }

  return SAFE_W - MODE_SMALL_W;
}

void drawBottomBar()
{
  int barY = SAFE_Y + SAFE_H - BOTTOM_BAR_H;
  int barBottom = SAFE_Y + SAFE_H - 1;
  int barH = barBottom - barY + 1;

  int heatW = bottomBarHeatW;
  int coolW = SAFE_W - heatW;

  int heatX = SAFE_X;
  int coolX = SAFE_X + heatW;

  String heatLabel = heatW > 150
    ? "Heating to " + formatSetPoint(latestStatus.heatSetPointFahr)
    : "Heat";

  String coolLabel = coolW > 150
    ? "Cooling"
    : "Cool";

  gfx->fillRect(SAFE_X, barY, SAFE_W, barH, HVAC_BG);

  gfx->fillRect(heatX, barY, heatW, barH, HVAC_HEAT_FILL);
  gfx->fillRect(coolX, barY, coolW, barH, HVAC_COOL_FILL);

  gfx->drawLine(SAFE_X, barY, SAFE_X + SAFE_W - 1, barY, HVAC_LINE);
  gfx->drawLine(SAFE_X, barBottom, SAFE_X + SAFE_W - 1, barBottom, HVAC_LINE);
  gfx->drawLine(coolX, barY, coolX, barBottom, HVAC_LINE);

  setFontMedium(HVAC_TEXT);

  drawCenteredTextInBox(
    heatX,
    barY,
    heatW,
    barH,
    heatLabel.c_str(),
    HVAC_TEXT);

  drawCenteredTextInBox(
    coolX,
    barY,
    coolW,
    barH,
    coolLabel.c_str(),
    HVAC_TEXT);
}

void animateBottomBarToSelectedMode()
{
  int startHeatW = bottomBarHeatW;
  int targetHeatW = getTargetHeatWidth();

  if (startHeatW == targetHeatW)
  {
    drawBottomBar();
    gfx->flush();
    return;
  }

  for (int step = 1; step <= MODE_ANIMATION_STEPS; step++)
  {
    float progress = (float)step / (float)MODE_ANIMATION_STEPS;

    // Smoothstep easing: starts and ends softer than a straight linear slide.
    progress = progress * progress * (3.0 - 2.0 * progress);

    bottomBarHeatW = startHeatW +
      (int)((targetHeatW - startHeatW) * progress);

    drawBottomBar();
    gfx->flush();

    delay(MODE_ANIMATION_DELAY_MS);
  }

  bottomBarHeatW = targetHeatW;
  drawBottomBar();
  gfx->flush();
}

void drawThermostatScreen()
{
  gfx->fillScreen(HVAC_BG);

  drawTopBar();
  drawLargeTemperature();
  drawHumidityPanel();
  drawBottomBar();

  gfx->flush();
}

void connectWiFi()
{
  Serial.print("Connecting to Wi-Fi");

  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);

  while (WiFi.status() != WL_CONNECTED)
  {
    delay(500);
    Serial.print(".");
  }

  Serial.println();
  Serial.print("Wi-Fi connected. IP: ");
  Serial.println(WiFi.localIP());
}

void publishUpstairsSensorReading()
{
  bool hasReading = readSht45();

  if (!hasReading)
  {
    Serial.println("Skipping upstairs sensor publish because SHT45 read failed.");
    return;
  }

  StaticJsonDocument<256> doc;

  doc["temperatureFahr"] = round(upstairsTemperatureF * 10.0) / 10.0;
  doc["relativeHumidity"] = round(upstairsRelativeHumidity * 10.0) / 10.0;
  doc["motionDetected"] = motionDetected;
  doc["mode"] = selectedMode;

  char buffer[256];
  size_t length = serializeJson(doc, buffer);

  bool published = mqttClient.publish(
    SENSOR_TOPIC,
    buffer,
    length
  );

  Serial.print("Published upstairs sensor message: ");
  Serial.println(published ? "yes" : "no");
  Serial.println(buffer);
}

void onMqttMessage(char* topic, byte* payload, unsigned int length)
{
  Serial.print("MQTT message on topic: ");
  Serial.println(topic);

  StaticJsonDocument<2048> doc;

  DeserializationError error = deserializeJson(doc, payload, length);

  if (error)
  {
    Serial.print("Failed to parse status JSON: ");
    Serial.println(error.c_str());
    return;
  }

  latestStatus.heat = doc["heat"] | false;
  latestStatus.cool = doc["cool"] | false;
  latestStatus.fan = doc["fan"] | false;

  latestStatus.reason = doc["reason"] | "";
  latestStatus.mode = doc["mode"] | "";

  latestStatus.heatSetPointFahr = doc["heatSetPointFahr"] | NAN;

  latestStatus.upstairsTemperature = doc["upstairsTemperature"] | NAN;
  latestStatus.upstairsAbsoluteHumidity = doc["upstairsAbsoluteHumidity"] | NAN;
  latestStatus.downstairsAbsoluteHumidity = doc["downstairsAbsoluteHumidity"] | NAN;
  latestStatus.outsideTemperature = doc["outsideTemperature"] | NAN;
  latestStatus.outsideAbsoluteHumidity = doc["outsideAbsoluteHumidity"] | NAN;

  Serial.println("Parsed HVAC status. Redrawing screen.");
  drawThermostatScreen();
}

void connectMqtt()
{
  while (!mqttClient.connected())
  {
    Serial.print("Connecting to MQTT...");

    String clientId = "waveshare-hvac-display-";
    clientId += String((uint32_t)ESP.getEfuseMac(), HEX);

    if (mqttClient.connect(clientId.c_str()))
    {
      Serial.println("connected");

      mqttClient.subscribe(STATUS_TOPIC);

      Serial.print("Subscribed to ");
      Serial.println(STATUS_TOPIC);
    }
    else
    {
      Serial.print("failed, rc=");
      Serial.print(mqttClient.state());
      Serial.println(". Retrying in 5 seconds.");

      delay(5000);
    }
  }
}

void scanMainI2CBus()
{
  Serial.println("Scanning main I2C bus SDA=21 SCL=22");

  for (uint8_t address = 1; address < 127; address++)
  {
    Wire.beginTransmission(address);
    uint8_t error = Wire.endTransmission();

    if (error == 0)
    {
      Serial.print("Found I2C device at 0x");

      if (address < 16)
      {
        Serial.print("0");
      }

      Serial.println(address, HEX);
    }

    delay(2);
  }

  Serial.println("I2C scan complete.");
}

void scanI2CBus(TwoWire& bus, int sda, int scl, const char* name)
{
  Serial.print("Scanning ");
  Serial.print(name);
  Serial.print(" SDA=");
  Serial.print(sda);
  Serial.print(" SCL=");
  Serial.println(scl);

  bus.begin(sda, scl);
  delay(100);

  bool foundAny = false;

  for (uint8_t address = 1; address < 127; address++)
  {
    bus.beginTransmission(address);
    uint8_t error = bus.endTransmission();

    if (error == 0)
    {
      foundAny = true;
      Serial.print("Found I2C device at 0x");

      if (address < 16)
      {
        Serial.print("0");
      }

      Serial.println(address, HEX);
    }
  }

  if (!foundAny)
  {
    Serial.println("No I2C devices found.");
  }

  Serial.println();
}

bool i2cAddressResponds(uint8_t address)
{
  Wire.beginTransmission(address);
  uint8_t error = Wire.endTransmission();

  return error == 0;
}

bool sht45AddressResponds(uint8_t address)
{
  Wire.beginTransmission(address);
  uint8_t error = Wire.endTransmission();

  return error == 0;
}

void initSht45()
{
  Serial.println("Initializing upstairs SHT45 on shared I2C bus GPIO8/GPIO7...");

  const uint8_t SHT45_ADDRESS = 0x44;

  if (!sht45AddressResponds(SHT45_ADDRESS))
  {
    Serial.println("No SHT45 response at I2C address 0x44 on GPIO7/GPIO8.");
    sht45Ready = false;
    return;
  }

  Serial.println("SHT45 address responded. Starting library...");

  if (!sht45.begin(&Wire))
  {
    Serial.println("Could not start SHT45 library.");
    sht45Ready = false;
    return;
  }

  sht45.setPrecision(SHT4X_HIGH_PRECISION);
  sht45.setHeater(SHT4X_NO_HEATER);

  sht45Ready = true;
  Serial.println("SHT45 initialized.");
}

bool readSht45()
{
  if (!sht45Ready)
  {
    return false;
  }

  sensors_event_t humidity;
  sensors_event_t temperature;

  if (!sht45.getEvent(&humidity, &temperature))
  {
    Serial.println("Failed to read SHT45.");
    return false;
  }

  upstairsTemperatureF = temperature.temperature * 9.0 / 5.0 + 32.0;
  upstairsRelativeHumidity = humidity.relative_humidity;

  Serial.print("SHT45 temp F: ");
  Serial.println(upstairsTemperatureF, 1);

  Serial.print("SHT45 RH: ");
  Serial.println(upstairsRelativeHumidity, 1);

  return true;
}

void scanSht45Bus()
{
  Serial.println("Scanning shared I2C bus SDA=8 SCL=7");

  for (uint8_t address = 1; address < 127; address++)
  {
    Wire.beginTransmission(address);
    uint8_t error = Wire.endTransmission();

    if (error == 0)
    {
      Serial.print("Found I2C device at 0x");

      if (address < 16)
      {
        Serial.print("0");
      }

      Serial.println(address, HEX);
    }

    delay(2);
  }

  Serial.println("Shared I2C scan complete.");
}

void initLd2410Out()
{
  pinMode(LD2410_OUT_PIN, INPUT);
  Serial.println("LD2410 OUT pin started.");
}

void pollLd2410Out()
{
  static bool lastMotionDetected = false;
  static unsigned long lastPrintMs = 0;

  motionDetected = digitalRead(LD2410_OUT_PIN) == HIGH;

  if (motionDetected != lastMotionDetected || millis() - lastPrintMs > 10000)
  {
    lastMotionDetected = motionDetected;
    lastPrintMs = millis();

    Serial.print("LD2410 presence OUT: ");
    Serial.println(motionDetected ? "yes" : "no");
  }
}

bool isValidScreenTouch(int x, int y)
{
  return x >= 0 &&
         x < SCREEN_W &&
         y >= 0 &&
         y < SCREEN_H;
}

void changeSelectedMode(const String& newMode)
{
  if (selectedMode == newMode)
  {
    return;
  }

  selectedMode = newMode;

  int startHeatW = bottomBarHeatW;
  int targetHeatW = getTargetHeatWidth();

  for (int step = 1; step <= MODE_ANIMATION_STEPS; step++)
  {
    float progress = (float)step / (float)MODE_ANIMATION_STEPS;
    progress = progress * progress * (3.0 - 2.0 * progress);

    bottomBarHeatW = startHeatW +
      (int)((targetHeatW - startHeatW) * progress);

    drawBottomBar();
    gfx->flush();

    delay(MODE_ANIMATION_DELAY_MS);
  }

  bottomBarHeatW = targetHeatW;

  drawBottomBar();
  gfx->flush();

  publishUpstairsSensorReading();
}

void handleTouchPress(int x, int y)
{
  int barY = SAFE_Y + SAFE_H - BOTTOM_BAR_H;
  int barBottom = SAFE_Y + SAFE_H - 1;

  bool inBottomBar =
    x >= SAFE_X &&
    x < SAFE_X + SAFE_W &&
    y >= barY &&
    y <= barBottom;

  if (!inBottomBar)
  {
    return;
  }

  int heatW = bottomBarHeatW;
  int dividerX = SAFE_X + heatW;

  if (x < dividerX)
  {
    Serial.println("Touch selected Heat");
    changeSelectedMode("Heat");
    return;
  }

  Serial.println("Touch selected Cool");
  changeSelectedMode("Cool");
}

void pollTouch()
{
  if (!touchReady)
  {
    return;
  }

  static bool touchWasDown = false;

  bsp_touch_read();

  touch_data_t touchData;

  bool hasTouch = bsp_touch_get_coordinates(&touchData);

  if (!hasTouch)
  {
    touchWasDown = false;
    return;
  }

  int rawX = touchData.coords[0].x;
  int rawY = touchData.coords[0].y;

  int x = rawY;
  int y = 320 - rawX;

  if (!isValidScreenTouch(x, y))
  {
    Serial.print("Ignored invalid touch X=");
    Serial.print(x);
    Serial.print(" Y=");
    Serial.println(y);
    return;
  }

  if (touchWasDown)
  {
    return;
  }

  touchWasDown = true;

  // Serial.print("Touch raw X=");
  // Serial.print(rawX);
  // Serial.print(" Y=");
  // Serial.print(rawY);
  // Serial.print(" -> screen X=");
  // Serial.print(x);
  // Serial.print(" Y=");
  // Serial.println(y);

  handleTouchPress(x, y);
}

void setup()
{
  Serial.begin(115200);
  delay(1000);

  initDisplay();
  initLd2410Out();

  bottomBarHeatW = getTargetHeatWidth();
  drawThermostatScreen();

  scanSht45Bus();
  initSht45();
  readSht45();

  connectWiFi();

  mqttClient.setBufferSize(2048);
  mqttClient.setServer(MQTT_HOST, MQTT_PORT);
  mqttClient.setCallback(onMqttMessage);

  connectMqtt();
}

void loop()
{
  pollTouch();
  mqttClient.loop();
  pollLd2410Out();

  if (WiFi.status() != WL_CONNECTED)
  {
    connectWiFi();
  }

  if (!mqttClient.connected())
  {
    connectMqtt();
  }

  unsigned long now = millis();

  if (now - lastPublishMs >= publishIntervalMs)
  {
    lastPublishMs = now;
    publishUpstairsSensorReading();
  }
}
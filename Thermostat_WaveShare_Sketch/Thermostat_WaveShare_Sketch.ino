#include <Arduino_GFX_Library.h>
#include <Adafruit_GFX.h>
#include <WiFi.h>
#include <PubSubClient.h>
#include <ArduinoJson.h>
#include "TCA9554.h"
#include "secrets.h"

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

  double upstairsTemperature = NAN;
  double upstairsAbsoluteHumidity = NAN;
  double downstairsAbsoluteHumidity = NAN;
  double outsideTemperature = NAN;
  double outsideAbsoluteHumidity = NAN;
};

HvacStatus latestStatus;

unsigned long lastPublishMs = 0;
const unsigned long publishIntervalMs = 10000;

void initDisplay()
{
  Wire.begin(21, 22);

  TCA.begin();
  TCA.pinMode1(1, OUTPUT);

  TCA.write1(1, 1);
  delay(10);
  TCA.write1(1, 0);
  delay(10);
  TCA.write1(1, 1);
  delay(200);

  if (!gfx->begin())
  {
    Serial.println("gfx->begin() failed!");
  }

  pinMode(GFX_BL, OUTPUT);
  digitalWrite(GFX_BL, HIGH);
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

void setFontBottom(uint16_t color)
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

  printAtString(24, 58, outside);
}

void drawLargeTemperature()
{
  const int areaX = 0;
  const int areaY = 72;
  const int areaW = 300;
  const int areaH = 175;

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

  int gap = 8;
  int totalW = bigW + gap + smallW;

  int startX = areaX + ((areaW - totalW) / 2);
  int bigBaselineY = areaY + ((areaH - bigH) / 2) - bigY1;

  gfx->setFont(&FreeSansBold24pt7b);
  gfx->setTextSize(2);
  gfx->setTextColor(HVAC_TEXT);
  gfx->setCursor(startX - bigX1, bigBaselineY);
  gfx->print(wholePart);

  int smallX = startX + bigW + gap;
  int smallBaselineY = bigBaselineY;

  gfx->setFont(&FreeSansBold24pt7b);
  gfx->setTextSize(1);
  gfx->setTextColor(HVAC_TEXT);
  gfx->setCursor(smallX - smallX1, smallBaselineY);
  gfx->print(decimalPart);
}

void drawHumidityPanel()
{
  int x = 315;

  setFontHeading(HVAC_TEXT);
  printAt(x, 112, "Humidity");

  setFontMedium(HVAC_TEXT);

  String out = "Out: ";
  out += formatOneDecimal(latestStatus.outsideAbsoluteHumidity);
  printAtString(x, 150, out);

  String up = "Up: ";
  up += formatOneDecimal(latestStatus.upstairsAbsoluteHumidity);
  printAtString(x, 185, up);

  String down = "Down: ";
  down += formatOneDecimal(latestStatus.downstairsAbsoluteHumidity);
  printAtString(x, 220, down);
}

void drawBottomBar()
{
  int w = gfx->width();
  int h = gfx->height();

  int barY = h - 58;
  int barH = h - barY;

  int heatW = 350;
  int coolW = w - heatW;

  gfx->fillRect(0, barY, heatW, barH, HVAC_HEAT_FILL);
  gfx->fillRect(heatW, barY, coolW, barH, HVAC_COOL_FILL);

  gfx->drawLine(0, barY, w - 1, barY, HVAC_LINE);
  gfx->drawLine(heatW, barY, heatW, h - 1, HVAC_LINE);

  setFontBottom(HVAC_TEXT);
  drawCenteredTextInBox(0, barY, heatW, barH, "Heating to 70", HVAC_TEXT);
  drawCenteredTextInBox(heatW, barY, coolW, barH, "Cool", HVAC_TEXT);
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

void publishFakeSensorReading()
{
  StaticJsonDocument<256> doc;

  doc["temperatureFahr"] = 70.0;
  doc["relativeHumidity"] = 50.0;
  doc["motionDetected"] = true;
  doc["mode"] = "Heat";

  char buffer[256];
  size_t length = serializeJson(doc, buffer);

  bool published = mqttClient.publish(
    SENSOR_TOPIC,
    buffer,
    length);

  Serial.print("Published upstairs sensor message: ");
  Serial.println(published ? "yes" : "no");
  Serial.println(buffer);
}

void onMqttMessage(char* topic, byte* payload, unsigned int length)
{
  Serial.print("MQTT message on topic: ");
  Serial.println(topic);

  StaticJsonDocument<1024> doc;

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

void setup()
{
  Serial.begin(115200);
  delay(1000);

  initDisplay();
  drawThermostatScreen();

  connectWiFi();

  mqttClient.setServer(MQTT_HOST, MQTT_PORT);
  mqttClient.setCallback(onMqttMessage);

  connectMqtt();
}

void loop()
{
  if (WiFi.status() != WL_CONNECTED)
  {
    connectWiFi();
  }

  if (!mqttClient.connected())
  {
    connectMqtt();
  }

  mqttClient.loop();

  unsigned long now = millis();

  if (now - lastPublishMs >= publishIntervalMs)
  {
    lastPublishMs = now;
    publishFakeSensorReading();
  }
}
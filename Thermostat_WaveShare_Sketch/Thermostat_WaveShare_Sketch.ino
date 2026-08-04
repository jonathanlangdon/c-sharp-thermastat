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

#define LCD_QSPI_CS 12
#define LCD_QSPI_CLK 5
#define LCD_QSPI_D0 1
#define LCD_QSPI_D1 2
#define LCD_QSPI_D2 3
#define LCD_QSPI_D3 4

#define ROTATION 1
#define GFX_BL 6

#define I2C_SDA 8
#define I2C_SCL 7

#define SHT45_SDA 17 // blue wire port 16 on rail
#define SHT45_SCL 9 // yellow wire port 14 on rail

#define SCREEN_W 480
#define SCREEN_H 320

#define SCREEN_MAIN 0
#define SCREEN_SETTINGS 1
#define SCREEN_STATS 2

int currentScreen = SCREEN_MAIN;

#define SAFE_X 45
#define SAFE_Y 55
#define SAFE_RIGHT_MARGIN 35
#define SAFE_BOTTOM_MARGIN 40
#define SAFE_W (SCREEN_W - SAFE_X - SAFE_RIGHT_MARGIN)
#define SAFE_H (SCREEN_H - SAFE_Y - SAFE_BOTTOM_MARGIN)
#define STATS_X (SAFE_X + 20)

// Settings Page Constants
#define SETTINGS_ARROW_X (SAFE_X + 14)
#define SETTINGS_ARROW_W 56
#define SETTINGS_ARROW_H 62

#define SETTINGS_EXIT_X SETTINGS_ARROW_X
#define SETTINGS_EXIT_Y SAFE_Y
#define SETTINGS_EXIT_W SETTINGS_ARROW_W
#define SETTINGS_EXIT_H 32

#define SETTINGS_ROW_START_Y (SAFE_Y + 4)
#define SETTINGS_ROW_GAP 32
#define SETTINGS_ROW_H 28

#define SETTINGS_VALUE_X (SAFE_X + SAFE_W - 120) // was 85
#define SETTINGS_VALUE_W 78
#define SETTINGS_VALUE_H 28

#define SETTINGS_LABEL_RIGHT_X (SETTINGS_VALUE_X - 12)

#define SETTINGS_FOOTER_Y (SAFE_Y + SAFE_H - 36)
#define SETTINGS_FOOTER_H 34
#define SETTINGS_FOOTER_BUTTON_GAP 10
#define SETTINGS_FOOTER_BUTTON_W ((SAFE_W - SETTINGS_FOOTER_BUTTON_GAP) / 2)

#define SETTINGS_MANUAL_OVERRIDE_X SAFE_X
#define SETTINGS_MANUAL_MODE_X (SAFE_X + SETTINGS_FOOTER_BUTTON_W + SETTINGS_FOOTER_BUTTON_GAP)

// Top Main Page Icons
#define TOP_ICON_CENTER_Y (SAFE_Y + 34)

#define STATS_CENTER_X (SAFE_X + 230)
#define WINDOW_BREEZE_CENTER_X (SAFE_X + 295)
#define GEAR_CENTER_X (SAFE_X + 365)

#define TOP_ICON_TOUCH_SIZE 56
#define TOP_ICON_BITMAP_W 40
#define TOP_ICON_BITMAP_H 40

#define GEAR_CENTER_Y TOP_ICON_CENTER_Y
#define GEAR_TOUCH_SIZE TOP_ICON_TOUCH_SIZE
#define GEAR_BITMAP_W 40
#define GEAR_BITMAP_H 40

#define LD2410_OUT_PIN 43

#define BOTTOM_BAR_H 42

#define MODE_SMALL_W 90
#define MODE_ANIMATION_STEPS 14
#define MODE_ANIMATION_DELAY_MS 16

#define SETTINGS_FLASH_INTERVAL_MS 500

#define SETTINGS_UP_ARROW_Y (SAFE_Y + 42)
#define SETTINGS_DOWN_ARROW_Y (SAFE_Y + 118)

enum SettingTarget {
  SETTING_TARGET_NONE,
  SETTING_TARGET_FAIR_HUMIDITY,
  SETTING_TARGET_GOOD_HUMIDITY,
  SETTING_TARGET_IDEAL_HUMIDITY,
  SETTING_TARGET_DAY_HEATING,
  SETTING_TARGET_NIGHT_HEATING
};

SettingTarget currentSettingTarget = SETTING_TARGET_NONE;

bool settingsFlashOn = true;
unsigned long lastSettingsFlashMs = 0;

int bottomBarHeatW = SAFE_W - MODE_SMALL_W;

bool touchReady = false;
unsigned long lastTouchLogMs = 0;

bool motionDetected = false;

Adafruit_SHT4x sht45 = Adafruit_SHT4x();

TwoWire sht45Wire = TwoWire(1);

bool sht45Ready = false;
double upstairsTemperatureF = NAN;
double upstairsRelativeHumidity = NAN;

String selectedMode = "Cool";

String pendingMode = "";
unsigned long pendingModeStartedMs = 0;
const unsigned long pendingModeHoldMs = 15000;

const char* MQTT_HOST = "hvac.local";
const int MQTT_PORT = 1883;

const char* SENSOR_TOPIC = "hvac/upstairs/sensor";
const char* STATUS_TOPIC = "hvac/status";
const char* MODE_SET_TOPIC = "hvac/mode/set";
const char* SETTINGS_SET_TOPIC = "hvac/settings/set";

bool windowToggle = false;
bool shouldOpenWindows = false;
bool pendingWindowManualOn = false;
bool pendingWindowManualOff = false;
unsigned long pendingWindowManualStartedMs = 0;
const unsigned long pendingWindowManualTimeoutMs = 15000;

unsigned long lastWindSoundMs = 0;
const unsigned long windSoundIntervalMs = 180000; // 3 minutes

TCA9554 TCA(0x20);

Arduino_DataBus* bus = new Arduino_ESP32QSPI(
  LCD_QSPI_CS,
  LCD_QSPI_CLK,
  LCD_QSPI_D0,
  LCD_QSPI_D1,
  LCD_QSPI_D2,
  LCD_QSPI_D3);

Arduino_GFX* g = new Arduino_AXS15231B(
  bus,
  -1,
  0,
  false,
  320,
  480);

Arduino_Canvas* gfx = new Arduino_Canvas(
  320,
  480,
  g,
  0,
  0,
  ROTATION);

WiFiClient wifiClient;
PubSubClient mqttClient(wifiClient);

#define HVAC_NORMAL_BG RGB565_BLACK
#define HVAC_NORMAL_TEXT RGB565_WHITE
#define HVAC_NORMAL_LINE 0x7BEF

#define HVAC_WINDOW_BG RGB565_WHITE
#define HVAC_WINDOW_TEXT 0x001F
#define HVAC_WINDOW_LINE 0x001F

uint16_t hvacBg = HVAC_NORMAL_BG;
uint16_t hvacText = HVAC_NORMAL_TEXT;
uint16_t hvacLine = HVAC_NORMAL_LINE;

#define HVAC_BG hvacBg
#define HVAC_TEXT hvacText
#define HVAC_LINE hvacLine

#define HVAC_HEAT_FILL 0xF800
#define HVAC_COOL_FILL 0x001F


struct HvacStatus {
  bool heat = false;
  bool cool = false;
  bool fan = false;
  bool manualOverride = false;
  String manualMode = "Off";
  
  String reason = "Waiting";
  String mode = "Heat";
  String now = "";

  double heatSetPointFahr = NAN;
  double heatSetPointDay = NAN;
  double heatSetPointNight = NAN;
  double maxAbsHumSetPoint = NAN;

  double upstairsTemperature = NAN;
  double upstairsRelativeHumidity = NAN;
  double upstairsAbsoluteHumidity = NAN;
  double downstairsTemperature = NAN;
  double downstairsRelativeHumidity = NAN;
  double downstairsAbsoluteHumidity = NAN;
  double outsideTemperature = NAN;
  double outsideAbsoluteHumidity = NAN;
  double controlAbsoluteHumidity = NAN;

  double upTempCalibration = NAN;
  double upRelHumCalibration = NAN;
  double downTempCalibration = NAN;
  double downRelHumCalibration = NAN;

  double coolHoursToday = NAN;
  double heatHoursToday = NAN;

  String lastHeatStarted = "";
  String lastHeatStopped = "";
  String lastCoolStarted = "";
  String lastCoolStopped = "";

  double humidityTargetFair = NAN;
  double humidityTargetGood = NAN;
  double humidityTargetIdeal = NAN;
};

HvacStatus latestStatus;

struct SettingsDraft {
  double heatSetPointDay = NAN;
  double heatSetPointNight = NAN;

  double humidityTargetFair = NAN;
  double humidityTargetGood = NAN;
  double humidityTargetIdeal = NAN;

  bool manualOverride = false;
  String manualMode = "Off";
};

SettingsDraft settingsDraft;
bool settingsDraftActive = false;

unsigned long lastPublishMs = 0;
const unsigned long publishIntervalMs = 30000;

unsigned long lastWiFiAttemptMs = 0;
unsigned long lastMqttAttemptMs = 0;

const unsigned long wifiRetryIntervalMs = 10000;
const unsigned long mqttRetryIntervalMs = 5000;

bool wifiWasConnected = false;
bool mqttWasConnected = false;

void applyManualOverrideLocally(bool manualOverride, const String& manualMode) {
  latestStatus.manualOverride = manualOverride;
  latestStatus.manualMode = manualMode;

  latestStatus.heat = false;
  latestStatus.cool = false;

  if (manualOverride && manualMode.equalsIgnoreCase("Fan")) {
    latestStatus.fan = true;
  } else {
    latestStatus.fan = false;
  }
}

void updateWindowToggleFromMqtt() {
  if (pendingWindowManualOn) {
    if (isManualFanStatus()) {
      Serial.println("Window manual fan confirmed by MQTT.");
      pendingWindowManualOn = false;
      return;
    }

    if (millis() - pendingWindowManualStartedMs > pendingWindowManualTimeoutMs) {
      Serial.println("Window manual fan command timed out.");
      pendingWindowManualOn = false;
      windowToggle = false;
    }

    return;
  }

  if (pendingWindowManualOff) {
    if (!latestStatus.manualOverride) {
      Serial.println("Window manual fan off confirmed by MQTT.");
      pendingWindowManualOff = false;
      windowToggle = false;
      return;
    }

    if (millis() - pendingWindowManualStartedMs > pendingWindowManualTimeoutMs) {
      Serial.println("Window manual fan off command timed out.");
      pendingWindowManualOff = false;
    }

    return;
  }

  // If the user turns off Manual Override from the settings page,
  // clear the local window latch once MQTT confirms it.
  if (!latestStatus.manualOverride) {
    windowToggle = false;
  }
}

void updateWindowTheme() {
  if (isWindowThemeActive()) {
    hvacBg = HVAC_WINDOW_BG;
    hvacText = HVAC_WINDOW_TEXT;
    hvacLine = HVAC_WINDOW_LINE;
    return;
  }

  hvacBg = HVAC_NORMAL_BG;
  hvacText = HVAC_NORMAL_TEXT;
  hvacLine = HVAC_NORMAL_LINE;
}

void initTouch() {
  Serial.println("Initializing AXS15231B touch...");

  bsp_touch_init(&Wire, -1, 0, 320, 480);

  touchReady = true;

  Serial.println("AXS15231B touch initialized.");
}


void initDisplay() {
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

  if (!gfx->begin()) {
    Serial.println("gfx->begin() failed! Stopping before drawing.");
    while (true) {
      delay(1000);
    }
  }

  pinMode(GFX_BL, OUTPUT);
  digitalWrite(GFX_BL, HIGH);

  initTouch();
}

void playWindSound() {
  Serial.println("TODO: play wind.wav");
}

void updateWindSound() {
  if (!shouldPlayWindSound()) {
    lastWindSoundMs = 0;
    return;
  }

  unsigned long now = millis();

  if (lastWindSoundMs == 0 ||
      now - lastWindSoundMs >= windSoundIntervalMs) {
    lastWindSoundMs = now;
    playWindSound();
  }
}


void setFontSmall(uint16_t color) {
  gfx->setFont(&FreeSans9pt7b);
  gfx->setTextSize(1);
  gfx->setTextColor(color);
}

void setFontMedium(uint16_t color) {
  gfx->setFont(&FreeSans12pt7b);
  gfx->setTextSize(1);
  gfx->setTextColor(color);
}

void setFontHeading(uint16_t color) {
  gfx->setFont(&FreeSansBold18pt7b);
  gfx->setTextSize(1);
  gfx->setTextColor(color);
}

void printAt(int x, int y, const char* text) {
  gfx->setCursor(x, y);
  gfx->print(text);
}

void printAtString(int x, int y, const String& text) {
  gfx->setCursor(x, y);
  gfx->print(text);
}

double roundToTenths(double value) {
  return round(value * 10.0) / 10.0;
}

double clampSettingValue(SettingTarget target, double value) {
  switch (target) {
    case SETTING_TARGET_FAIR_HUMIDITY:
    case SETTING_TARGET_GOOD_HUMIDITY:
    case SETTING_TARGET_IDEAL_HUMIDITY:
      return constrain(value, 0.0, 30.0);

    case SETTING_TARGET_DAY_HEATING:
    case SETTING_TARGET_NIGHT_HEATING:
      return constrain(value, 50.0, 85.0);

    default:
      return value;
  }
}

double getCurrentSettingTargetValue() {
  switch (currentSettingTarget) {
    case SETTING_TARGET_FAIR_HUMIDITY:
      return settingsDraft.humidityTargetFair;

    case SETTING_TARGET_GOOD_HUMIDITY:
      return settingsDraft.humidityTargetGood;

    case SETTING_TARGET_IDEAL_HUMIDITY:
      return settingsDraft.humidityTargetIdeal;

    case SETTING_TARGET_DAY_HEATING:
      return settingsDraft.heatSetPointDay;

    case SETTING_TARGET_NIGHT_HEATING:
      return settingsDraft.heatSetPointNight;

    default:
      return NAN;
  }
}

void copySettingsDraftFromLatestStatus() {
  settingsDraft.heatSetPointDay = latestStatus.heatSetPointDay;
  settingsDraft.heatSetPointNight = latestStatus.heatSetPointNight;

  settingsDraft.humidityTargetFair = latestStatus.humidityTargetFair;
  settingsDraft.humidityTargetGood = latestStatus.humidityTargetGood;
  settingsDraft.humidityTargetIdeal = latestStatus.humidityTargetIdeal;

  settingsDraft.manualOverride = latestStatus.manualOverride;
  settingsDraft.manualMode = latestStatus.manualMode;

  settingsDraftActive = true;
}

void setCurrentSettingTargetValue(double value) {
  switch (currentSettingTarget) {
    case SETTING_TARGET_FAIR_HUMIDITY:
      settingsDraft.humidityTargetFair = value;
      break;

    case SETTING_TARGET_GOOD_HUMIDITY:
      settingsDraft.humidityTargetGood = value;
      break;

    case SETTING_TARGET_IDEAL_HUMIDITY:
      settingsDraft.humidityTargetIdeal = value;
      break;

    case SETTING_TARGET_DAY_HEATING:
      settingsDraft.heatSetPointDay = value;
      break;

    case SETTING_TARGET_NIGHT_HEATING:
      settingsDraft.heatSetPointNight = value;
      break;

    default:
      break;
  }
}

void adjustCurrentSettingTarget(double delta) {
  if (currentSettingTarget == SETTING_TARGET_NONE) {
    return;
  }

  double value = getCurrentSettingTargetValue();

  if (isnan(value)) {
    return;
  }

  value = roundToTenths(value + delta);
  value = clampSettingValue(currentSettingTarget, value);

  setCurrentSettingTargetValue(value);

  settingsFlashOn = true;
  lastSettingsFlashMs = millis();

  drawSettingsScreen();
}

String formatTwoDecimals(double value) {
  if (isnan(value)) {
    return "--";
  }

  return String(value, 2);
}

String formatDateTimeShort(const String& value) {
  if (value.length() < 16) {
    return "--";
  }

  // Example input: 2026-08-04T10:16:47
  // Output: 08-04 10:16
  return value.substring(5, 10) + " " + value.substring(11, 16);
}

String formatSetPoint(double value) {
  if (isnan(value)) {
    return "--";
  }

  return String(value, 1);
}

String formatBoolOnOff(bool value) {
  return value ? "On" : "Off";
}

String formatOneDecimal(double value) {
  if (isnan(value)) {
    return "--";
  }

  return String(value, 1);
}

String currentActivityText() {

  if (latestStatus.manualOverride &&
      latestStatus.manualMode.equalsIgnoreCase("Off")) {
    return "Status: System Off";
  }

  if (latestStatus.manualOverride &&
      latestStatus.manualMode.equalsIgnoreCase("Fan")) {
    return "Status: Manual Fan";
  }

  if (latestStatus.heat) {
    return "Status: Heating";
  }

  if (latestStatus.cool) {
    return "Status: Cooling";
  }

  if (latestStatus.fan) {
    return "Status: Fan";
  }

  return "Status: System Off";
}

void getBounds(
  const char* text,
  const GFXfont* font,
  int size,
  int16_t* x1,
  int16_t* y1,
  uint16_t* w,
  uint16_t* h) {
  gfx->setFont(font);
  gfx->setTextSize(size);
  gfx->getTextBounds(text, 0, 0, x1, y1, w, h);
}

void drawStatsFullRow(
  int y,
  const char* label,
  const String& value) {
  gfx->fillRect(SAFE_X, y - 16, SAFE_W, 22, HVAC_BG);

  setFontSmall(HVAC_TEXT);

  String text = String(label) + ": " + value;

  printAtString(SAFE_X + 88, y, text);
}

void drawStatsColumnRow(
  int x,
  int y,
  int w,
  const char* label,
  const String& value) {
  gfx->fillRect(x, y - 15, w, 20, HVAC_BG);

  setFontSmall(HVAC_TEXT);

  String text = String(label) + ": " + value;

  printAtString(x, y, text);
}

void drawUpArrowButton(int x, int y, int w, int h) {
  gfx->drawRect(x, y, w, h, HVAC_LINE);

  int cx = x + (w / 2);

  gfx->fillTriangle(
    cx,
    y + 10,
    x + 10,
    y + h - 10,
    x + w - 10,
    y + h - 10,
    HVAC_TEXT);
}

void drawDownArrowButton(int x, int y, int w, int h) {
  gfx->drawRect(x, y, w, h, HVAC_LINE);

  int cx = x + (w / 2);

  gfx->fillTriangle(
    x + 10,
    y + 10,
    x + w - 10,
    y + 10,
    cx,
    y + h - 10,
    HVAC_TEXT);
}

void drawSettingsFooterButton(
  int x,
  int y,
  int w,
  int h,
  const char* label,
  const String& value) {
  gfx->drawRect(x, y, w, h, HVAC_LINE);

  setFontSmall(HVAC_TEXT);

  String text = String(label) + ": " + value;

  drawCenteredTextInBox(
    x,
    y,
    w,
    h,
    text.c_str(),
    HVAC_TEXT);
}

void drawSettingsValueBox(
  int x,
  int y,
  int w,
  int h,
  const String& value,
  bool selected) {
  gfx->fillRect(x - 3, y - 3, w + 6, h + 6, HVAC_BG);

  if (selected && !settingsFlashOn) {
    return;
  }

  if (selected) {
    gfx->drawRect(x - 3, y - 3, w + 6, h + 6, HVAC_TEXT);
  }

  gfx->drawRect(x, y, w, h, HVAC_LINE);

  setFontMedium(HVAC_TEXT);

  int16_t x1;
  int16_t y1;
  uint16_t textW;
  uint16_t textH;

  gfx->getTextBounds(value.c_str(), 0, 0, &x1, &y1, &textW, &textH);

  int textX = x + ((w - textW) / 2) - x1;
  int textY = y + ((h - textH) / 2) - y1;

  gfx->setCursor(textX, textY);
  gfx->print(value);
}

void drawSettingsRow(
  int y,
  const char* label,
  const String& value,
  SettingTarget target) {
  setFontSmall(HVAC_TEXT);

  int16_t x1;
  int16_t y1;
  uint16_t textW;
  uint16_t textH;

  gfx->getTextBounds(label, 0, 0, &x1, &y1, &textW, &textH);

  int labelX = SETTINGS_LABEL_RIGHT_X - textW - x1;
  int labelY = y + 22;

  printAt(labelX, labelY, label);

  drawSettingsValueBox(
    SETTINGS_VALUE_X,
    y,
    SETTINGS_VALUE_W,
    SETTINGS_VALUE_H,
    value,
    currentSettingTarget == target);
}

void drawCenteredTextInBox(
  int boxX,
  int boxY,
  int boxW,
  int boxH,
  const char* text,
  uint16_t color) {
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

static const unsigned char statsBitmap[] PROGMEM = {
  0x00, 0x00, 0x00, 0x00, 0x00,
  0x00, 0x07, 0xC0, 0x00, 0x00,
  0x00, 0x0F, 0xC0, 0x00, 0x00,
  0x00, 0x0F, 0xC0, 0x00, 0x00,
  0x00, 0x0F, 0xC0, 0x00, 0x00,
  0x00, 0x0F, 0xC0, 0x00, 0x00,
  0x00, 0x0F, 0xC0, 0x00, 0x00,
  0x00, 0x0F, 0xC0, 0x00, 0x00,
  0x00, 0x0F, 0xC0, 0x00, 0x00,
  0x00, 0x0F, 0xC0, 0x00, 0x00,
  0x00, 0x0F, 0xC0, 0x00, 0x00,
  0x00, 0x0F, 0xC0, 0x00, 0x00,
  0x00, 0x0F, 0xC0, 0x01, 0xF8,
  0x00, 0x0F, 0xC0, 0x01, 0xF8,
  0x00, 0x0F, 0xC0, 0x01, 0xF8,
  0x00, 0x0F, 0xC0, 0x01, 0xF8,
  0x00, 0x0F, 0xC0, 0x01, 0xF8,
  0x00, 0x0F, 0xC0, 0x01, 0xF8,
  0x00, 0x0F, 0xC0, 0x01, 0xF8,
  0x00, 0x0F, 0xC0, 0x01, 0xF8,
  0x00, 0x0F, 0xC0, 0x01, 0xF8,
  0x00, 0x0F, 0xC0, 0x01, 0xF8,
  0x00, 0x0F, 0xC3, 0xF1, 0xF8,
  0x00, 0x0F, 0xC3, 0xF1, 0xF8,
  0x00, 0x0F, 0xC3, 0xF1, 0xF8,
  0x00, 0x0F, 0xC3, 0xF1, 0xF8,
  0x00, 0x0F, 0xC3, 0xF1, 0xF8,
  0x00, 0x0F, 0xC3, 0xF1, 0xF8,
  0x1F, 0x8F, 0xC3, 0xF1, 0xF8,
  0x1F, 0x8F, 0xC3, 0xF1, 0xF8,
  0x1F, 0x8F, 0xC3, 0xF1, 0xF8,
  0x1F, 0x8F, 0xC3, 0xF1, 0xF8,
  0x1F, 0x8F, 0xC3, 0xF1, 0xF8,
  0x1F, 0x8F, 0xC3, 0xF1, 0xF8,
  0x1F, 0x8F, 0xC3, 0xF1, 0xF8,
  0x1F, 0x8F, 0xC3, 0xF1, 0xF8,
  0x1F, 0x8F, 0xC3, 0xF1, 0xF8,
  0x1F, 0x8F, 0xC3, 0xF1, 0xF8,
  0x1F, 0x87, 0xC3, 0xE1, 0xF8,
  0x00, 0x00, 0x00, 0x00, 0x00
};

static const unsigned char windowBreezeBitmap[] PROGMEM = {
  0x00, 0x00, 0x00, 0x00, 0x00,
  0x00, 0x00, 0x00, 0x00, 0x00,
  0x00, 0x00, 0x00, 0x00, 0x00,
  0x00, 0x00, 0x00, 0x00, 0x00,
  0x00, 0x00, 0x00, 0x00, 0x00,
  0x00, 0x00, 0x00, 0x00, 0x00,
  0x00, 0x0F, 0xFC, 0x7F, 0xFC,
  0x00, 0x1F, 0xFE, 0xFF, 0xFE,
  0x00, 0x1C, 0x00, 0x60, 0x0E,
  0x00, 0x18, 0x00, 0x40, 0x06,
  0x00, 0x08, 0x00, 0x40, 0x06,
  0x00, 0x00, 0x00, 0x40, 0x06,
  0x00, 0x00, 0x00, 0x40, 0x06,
  0x00, 0x7C, 0x00, 0x40, 0x06,
  0x00, 0xEC, 0x00, 0x40, 0x06,
  0x00, 0xC4, 0x00, 0x40, 0x06,
  0x00, 0xC0, 0x00, 0x40, 0x06,
  0x00, 0xFF, 0xFC, 0x40, 0x06,
  0x00, 0x7F, 0xFC, 0x40, 0x06,
  0x00, 0x00, 0x00, 0x40, 0x06,
  0x00, 0x00, 0x00, 0x40, 0x06,
  0x1F, 0xFF, 0xF8, 0x40, 0x06,
  0x3F, 0xFF, 0xF0, 0x40, 0x06,
  0x60, 0x00, 0x00, 0x40, 0x06,
  0x63, 0x09, 0xE0, 0x40, 0x06,
  0x3F, 0x19, 0xE0, 0x40, 0x06,
  0x3E, 0x18, 0x00, 0x40, 0x06,
  0x00, 0x18, 0x00, 0x40, 0x06,
  0x00, 0x18, 0x00, 0x40, 0x06,
  0x00, 0x18, 0x00, 0x40, 0x06,
  0x00, 0x18, 0x00, 0x40, 0x06,
  0x00, 0x1C, 0x00, 0x60, 0x0E,
  0x00, 0x1F, 0xFE, 0xFF, 0xFE,
  0x00, 0x0F, 0xF0, 0x20, 0x08,
  0x00, 0x00, 0x00, 0x00, 0x00,
  0x00, 0x00, 0x00, 0x00, 0x00,
  0x00, 0x00, 0x00, 0x00, 0x00,
  0x00, 0x00, 0x00, 0x00, 0x00,
  0x00, 0x00, 0x00, 0x00, 0x00,
  0x00, 0x00, 0x00, 0x00, 0x00
};

static const unsigned char gearBitmap[] PROGMEM = {
  0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x07, 0x80, 0xE0, 0x00,
  0x00, 0x3F, 0x81, 0xFC, 0x00, 0x00, 0x7F, 0x81, 0xFE, 0x00,
  0x00, 0x7F, 0xC3, 0xFE, 0x00, 0x00, 0x3F, 0xFF, 0xFC, 0x00,
  0x00, 0x3F, 0xFF, 0xFC, 0x00, 0x00, 0x1F, 0xFF, 0xFC, 0x00,
  0x00, 0x1F, 0xFF, 0xF8, 0x00, 0x18, 0x3F, 0xFF, 0xFC, 0x08,
  0x3E, 0x7F, 0xFF, 0xFE, 0x7C, 0x3F, 0xFF, 0xFF, 0xFF, 0xFC,
  0x3F, 0xFF, 0xC3, 0xFF, 0xFE, 0x7F, 0xFF, 0x00, 0xFF, 0xFE,
  0x7F, 0xFE, 0x00, 0x3F, 0xFF, 0xFF, 0xFC, 0x00, 0x1F, 0xFF,
  0x7F, 0xF8, 0x00, 0x1F, 0xFE, 0x0F, 0xF8, 0x00, 0x0F, 0xF8,
  0x07, 0xF0, 0x00, 0x0F, 0xE0, 0x07, 0xF0, 0x00, 0x0F, 0xE0,
  0x07, 0xF0, 0x00, 0x0F, 0xE0, 0x07, 0xF0, 0x00, 0x0F, 0xE0,
  0x0F, 0xF8, 0x00, 0x0F, 0xF0, 0x3F, 0xF8, 0x00, 0x1F, 0xFC,
  0x7F, 0xFC, 0x00, 0x1F, 0xFF, 0x7F, 0xFC, 0x00, 0x3F, 0xFF,
  0x7F, 0xFE, 0x00, 0x7F, 0xFE, 0x3F, 0xFF, 0x81, 0xFF, 0xFE,
  0x3F, 0xFF, 0xFF, 0xFF, 0xFC, 0x3F, 0x7F, 0xFF, 0xFF, 0x7C,
  0x18, 0x3F, 0xFF, 0xFE, 0x18, 0x00, 0x1F, 0xFF, 0xFC, 0x00,
  0x00, 0x1F, 0xFF, 0xF8, 0x00, 0x00, 0x3F, 0xFF, 0xFC, 0x00,
  0x00, 0x3F, 0xFF, 0xFC, 0x00, 0x00, 0x3F, 0xC3, 0xFE, 0x00,
  0x00, 0x7F, 0xC1, 0xFE, 0x00, 0x00, 0x3F, 0x81, 0xFC, 0x00,
  0x00, 0x0F, 0x80, 0xF0, 0x00, 0x00, 0x03, 0x00, 0xC0, 0x00
};

void drawTopIcon(
  int centerX,
  int centerY,
  const unsigned char* bitmap) {
  int x = centerX - (TOP_ICON_BITMAP_W / 2);
  int y = centerY - (TOP_ICON_BITMAP_H / 2);

  gfx->drawBitmap(
    x,
    y,
    bitmap,
    TOP_ICON_BITMAP_W,
    TOP_ICON_BITMAP_H,
    HVAC_TEXT);
}

void drawGearIcon() {
  int x = GEAR_CENTER_X - (GEAR_BITMAP_W / 2);
  int y = GEAR_CENTER_Y - (GEAR_BITMAP_H / 2);

  gfx->drawBitmap(
    x,
    y,
    gearBitmap,
    GEAR_BITMAP_W,
    GEAR_BITMAP_H,
    HVAC_TEXT);
}

void drawStatsIcon() {
  drawTopIcon(
    STATS_CENTER_X,
    TOP_ICON_CENTER_Y,
    statsBitmap);
}

void drawWindowBreezeIcon() {
  drawTopIcon(
    WINDOW_BREEZE_CENTER_X,
    TOP_ICON_CENTER_Y,
    windowBreezeBitmap);
}

void drawTopBar() {
  setFontMedium(HVAC_TEXT);

  String outside = "Outside: ";
  outside += formatOneDecimal(latestStatus.outsideTemperature);

  printAtString(SAFE_X + 10, SAFE_Y + 20, outside);
}

void drawLargeTemperature() {
  const int areaX = SAFE_X + 30;
  const int areaY = SAFE_Y + 32;
  const int areaW = 245;
  const int areaH = 125;

  String temp = formatOneDecimal(latestStatus.upstairsTemperature);

  String wholePart = "--";
  String decimalPart = "";

  int dotIndex = temp.indexOf('.');

  if (dotIndex >= 0) {
    wholePart = temp.substring(0, dotIndex);
    decimalPart = temp.substring(dotIndex);
  } else {
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

void drawHumidityPanel() {
  int x = SAFE_X + 290;  // was 268
  int y = SAFE_Y + 90; // was 48

  setFontMedium(HVAC_TEXT);
  printAt(x, y, "Humidity");

  setFontSmall(HVAC_TEXT);

  String out = "Out: ";
  out += formatOneDecimal(latestStatus.outsideAbsoluteHumidity);
  printAtString(x, y + 25, out);

  String up = "Up: ";
  up += formatOneDecimal(latestStatus.upstairsAbsoluteHumidity);
  printAtString(x, y + 50, up);

  String down = "Down: ";
  down += formatOneDecimal(latestStatus.downstairsAbsoluteHumidity);
  printAtString(x, y + 75, down);
}

void drawCurrentActivity() {
  int x = SAFE_X + 10;  // was 268
  int y = SAFE_Y + 170;

  setFontSmall(HVAC_TEXT);

  String activity = currentActivityText();

  printAtString(x, y, activity);
}

int getLatestStatusHour() {
  if (latestStatus.now.length() < 13) {
    return -1;
  }

  return latestStatus.now.substring(11, 13).toInt();
}

bool isWindowReminderTime() {
  int hour = getLatestStatusHour();

  return hour >= 6 && hour < 22;
}

bool calculateShouldOpenWindows() {
  bool conditionOne =
    !isnan(latestStatus.upstairsTemperature) &&
    !isnan(latestStatus.outsideAbsoluteHumidity) &&
    !isnan(latestStatus.controlAbsoluteHumidity) &&
    latestStatus.upstairsTemperature > 70.0 &&
    latestStatus.outsideAbsoluteHumidity < 10.0 &&
    latestStatus.controlAbsoluteHumidity > 9.0;

  bool conditionTwo =
    !isnan(latestStatus.outsideAbsoluteHumidity) &&
    !isnan(latestStatus.outsideTemperature) &&
    latestStatus.outsideAbsoluteHumidity < 20.0 &&  // should be 10
    latestStatus.outsideTemperature > 60.0;

  return conditionOne || conditionTwo;
}

bool isManualFanStatus() {
  return latestStatus.manualOverride &&
         latestStatus.manualMode.equalsIgnoreCase("Fan");
}

bool isWindowModeActive() {
  return windowToggle &&
         (isManualFanStatus() || pendingWindowManualOn);
}

bool isWindowPromptActive() {
  return !isWindowModeActive() &&
         !latestStatus.manualOverride &&
         !windowToggle &&
         isWindowReminderTime() &&
         shouldOpenWindows;
}

bool isWindowThemeActive() {
  return isWindowModeActive() || isWindowPromptActive();
}

bool shouldPlayWindSound() {
  return isWindowPromptActive();
}

int getTargetHeatWidth() {
  if (selectedMode == "Cool") {
    return MODE_SMALL_W;
  }

  return SAFE_W - MODE_SMALL_W;
}

String normalizeMode(const String& mode) {
  if (mode.equalsIgnoreCase("Heat")) {
    return "Heat";
  }

  if (mode.equalsIgnoreCase("Cool")) {
    return "Cool";
  }

  return "";
}

void updateSettingsFlash() {
  if (currentScreen != SCREEN_SETTINGS) {
    return;
  }

  if (currentSettingTarget == SETTING_TARGET_NONE) {
    return;
  }

  unsigned long now = millis();

  if (now - lastSettingsFlashMs < SETTINGS_FLASH_INTERVAL_MS) {
    return;
  }

  settingsFlashOn = !settingsFlashOn;
  lastSettingsFlashMs = now;

  drawSettingsScreen();
}

bool updateSelectedModeFromStatus() {
  String statusMode = normalizeMode(latestStatus.mode);

  if (statusMode == "") {
    return false;
  }

  if (pendingMode != "") {
    unsigned long pendingAgeMs = millis() - pendingModeStartedMs;

    if (statusMode == pendingMode) {
      Serial.print("Mode command confirmed by hvac/status: ");
      Serial.println(statusMode);

      pendingMode = "";

      if (selectedMode == statusMode) {
        return false;
      }

      selectedMode = statusMode;
      return true;
    }

    if (pendingAgeMs <= pendingModeHoldMs) {
      Serial.print("Holding requested mode while waiting for confirmation. requested=");
      Serial.print(pendingMode);
      Serial.print(" status=");
      Serial.print(statusMode);
      Serial.print(" ageMs=");
      Serial.println(pendingAgeMs);

      selectedMode = pendingMode;
      bottomBarHeatW = getTargetHeatWidth();

      return false;
    }

    Serial.print("Mode command timed out. Accepting status mode: ");
    Serial.println(statusMode);

    pendingMode = "";
  }

  if (selectedMode == statusMode) {
    return false;
  }

  selectedMode = statusMode;
  return true;
}

void drawBottomBar() {
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
                       ? "Cooling to " + formatSetPoint(latestStatus.maxAbsHumSetPoint)
                       : "Cool";

  gfx->fillRect(SAFE_X, barY, SAFE_W, barH, HVAC_BG);

  gfx->fillRect(heatX, barY, heatW, barH, HVAC_HEAT_FILL);
  gfx->fillRect(coolX, barY, coolW, barH, HVAC_COOL_FILL);

  gfx->drawLine(SAFE_X, barY, SAFE_X + SAFE_W - 1, barY, HVAC_LINE);
  gfx->drawLine(SAFE_X, barBottom, SAFE_X + SAFE_W - 1, barBottom, HVAC_LINE);
  gfx->drawLine(coolX, barY, coolX, barBottom, HVAC_LINE);

  setFontMedium(HVAC_NORMAL_TEXT);

  drawCenteredTextInBox(
    heatX,
    barY,
    heatW,
    barH,
    heatLabel.c_str(),
    HVAC_NORMAL_TEXT);

  drawCenteredTextInBox(
    coolX,
    barY,
    coolW,
    barH,
    coolLabel.c_str(),
    HVAC_NORMAL_TEXT);
}

void animateBottomBarToSelectedMode() {
  int startHeatW = bottomBarHeatW;
  int targetHeatW = getTargetHeatWidth();

  if (startHeatW == targetHeatW) {
    drawBottomBar();
    gfx->flush();
    return;
  }

  for (int step = 1; step <= MODE_ANIMATION_STEPS; step++) {
    float progress = (float)step / (float)MODE_ANIMATION_STEPS;

    // Smoothstep easing: starts and ends softer than a straight linear slide.
    progress = progress * progress * (3.0 - 2.0 * progress);

    bottomBarHeatW = startHeatW + (int)((targetHeatW - startHeatW) * progress);

    drawBottomBar();
    gfx->flush();

    delay(MODE_ANIMATION_DELAY_MS);
  }

  bottomBarHeatW = targetHeatW;
  drawBottomBar();
  gfx->flush();
}

void drawThermostatScreen() {
  updateWindowTheme();

  gfx->fillScreen(HVAC_BG);

  drawTopBar();
  drawStatsIcon();
  drawWindowBreezeIcon();
  drawGearIcon();
  drawLargeTemperature();
  drawHumidityPanel();
  drawCurrentActivity();
  drawBottomBar();

  gfx->flush();
}

void drawStatsScreen() {
  gfx->fillScreen(HVAC_BG);

  // Exit button
  gfx->drawRect(
    SETTINGS_EXIT_X,
    SETTINGS_EXIT_Y,
    SETTINGS_EXIT_W,
    SETTINGS_EXIT_H,
    HVAC_LINE);

  setFontMedium(HVAC_TEXT);
  drawCenteredTextInBox(
    SETTINGS_EXIT_X,
    SETTINGS_EXIT_Y,
    SETTINGS_EXIT_W,
    SETTINGS_EXIT_H,
    "Exit",
    HVAC_TEXT);

  drawStatsFullRow(
    SAFE_Y + 52,
    "Hvac Status",
    latestStatus.reason);

  int colGap = 10;
  int colW = (SAFE_W - 10 - colGap) / 2;

  int leftX = STATS_X;
  int rightX = STATS_X + colW + colGap;

  int startY = SAFE_Y + 80;
  int rowGap = 18;

  drawStatsColumnRow(
    leftX,
    startY,
    colW,
    "Up temp cal",
    formatOneDecimal(latestStatus.upTempCalibration));

  drawStatsColumnRow(
    leftX,
    startY + rowGap,
    colW,
    "Up RH cal",
    formatOneDecimal(latestStatus.upRelHumCalibration));

  drawStatsColumnRow(
    leftX,
    startY + (rowGap * 2),
    colW,
    "Down temp cal",
    formatOneDecimal(latestStatus.downTempCalibration));

  drawStatsColumnRow(
    leftX,
    startY + (rowGap * 3),
    colW,
    "Down RH cal",
    formatOneDecimal(latestStatus.downRelHumCalibration));

  drawStatsColumnRow(
    leftX,
    startY + (rowGap * 4),
    colW,
    "Up temp",
    formatOneDecimal(latestStatus.upstairsTemperature));

  drawStatsColumnRow(
    leftX,
    startY + (rowGap * 5),
    colW,
    "Up RH",
    formatOneDecimal(latestStatus.upstairsRelativeHumidity));

  drawStatsColumnRow(
    leftX,
    startY + (rowGap * 6),
    colW,
    "Up abs hum",
    formatTwoDecimals(latestStatus.upstairsAbsoluteHumidity));

  drawStatsColumnRow(
    leftX,
    startY + (rowGap * 7),
    colW,
    "Cool hrs",
    formatTwoDecimals(latestStatus.coolHoursToday));

  drawStatsColumnRow(
    rightX,
    startY,
    colW,
    "Down temp",
    formatOneDecimal(latestStatus.downstairsTemperature));

  drawStatsColumnRow(
    rightX,
    startY + rowGap,
    colW,
    "Down RH",
    formatOneDecimal(latestStatus.downstairsRelativeHumidity));

  drawStatsColumnRow(
    rightX,
    startY + (rowGap * 2),
    colW,
    "Down abs hum",
    formatTwoDecimals(latestStatus.downstairsAbsoluteHumidity));

  drawStatsColumnRow(
    rightX,
    startY + (rowGap * 3),
    colW,
    "Heat hrs",
    formatTwoDecimals(latestStatus.heatHoursToday));

  drawStatsColumnRow(
    rightX,
    startY + (rowGap * 4),
    colW,
    "Heat start",
    formatDateTimeShort(latestStatus.lastHeatStarted));

  drawStatsColumnRow(
    rightX,
    startY + (rowGap * 5),
    colW,
    "Heat stop",
    formatDateTimeShort(latestStatus.lastHeatStopped));

  drawStatsColumnRow(
    rightX,
    startY + (rowGap * 6),
    colW,
    "Cool start",
    formatDateTimeShort(latestStatus.lastCoolStarted));

  drawStatsColumnRow(
    rightX,
    startY + (rowGap * 7),
    colW,
    "Cool stop",
    formatDateTimeShort(latestStatus.lastCoolStopped));

  gfx->flush();
}

void drawSettingsScreen() {
  gfx->fillScreen(HVAC_BG);

  // Optional safe-area border while testing:
  // gfx->drawRect(SAFE_X, SAFE_Y, SAFE_W, SAFE_H, HVAC_LINE);

  // Exit button
  gfx->drawRect(
    SETTINGS_EXIT_X,
    SETTINGS_EXIT_Y,
    SETTINGS_EXIT_W,
    SETTINGS_EXIT_H,
    HVAC_LINE);

  setFontMedium(HVAC_TEXT);
  drawCenteredTextInBox(
    SETTINGS_EXIT_X,
    SETTINGS_EXIT_Y,
    SETTINGS_EXIT_W,
    SETTINGS_EXIT_H,
    "Exit",
    HVAC_TEXT);

  drawUpArrowButton(
    SETTINGS_ARROW_X,
    SETTINGS_UP_ARROW_Y,
    SETTINGS_ARROW_W,
    SETTINGS_ARROW_H);

  drawDownArrowButton(
    SETTINGS_ARROW_X,
    SETTINGS_DOWN_ARROW_Y,
    SETTINGS_ARROW_W,
    SETTINGS_ARROW_H);

  int rowY = SETTINGS_ROW_START_Y;

  drawSettingsRow(
    rowY,
    "Fair humidity",
    formatSetPoint(settingsDraft.humidityTargetFair),
    SETTING_TARGET_FAIR_HUMIDITY);

  drawSettingsRow(
    rowY + SETTINGS_ROW_GAP,
    "Good humidity",
    formatSetPoint(settingsDraft.humidityTargetGood),
    SETTING_TARGET_GOOD_HUMIDITY);

  drawSettingsRow(
    rowY + (SETTINGS_ROW_GAP * 2),
    "Ideal humidity",
    formatSetPoint(settingsDraft.humidityTargetIdeal),
    SETTING_TARGET_IDEAL_HUMIDITY);

  drawSettingsRow(
    rowY + (SETTINGS_ROW_GAP * 3),
    "Day heating",
    formatSetPoint(settingsDraft.heatSetPointDay),
    SETTING_TARGET_DAY_HEATING);

  drawSettingsRow(
    rowY + (SETTINGS_ROW_GAP * 4),
    "Night heating",
    formatSetPoint(settingsDraft.heatSetPointNight),
    SETTING_TARGET_NIGHT_HEATING);

  // Footer row
  drawSettingsFooterButton(
    SETTINGS_MANUAL_OVERRIDE_X,
    SETTINGS_FOOTER_Y,
    SETTINGS_FOOTER_BUTTON_W,
    SETTINGS_FOOTER_H,
    "Manual Override",
    formatBoolOnOff(settingsDraft.manualOverride));

  drawSettingsFooterButton(
    SETTINGS_MANUAL_MODE_X,
    SETTINGS_FOOTER_Y,
    SETTINGS_FOOTER_BUTTON_W,
    SETTINGS_FOOTER_H,
    "Manual Mode",
    settingsDraft.manualMode);

  gfx->flush();
}

void startWiFiConnection() {
  Serial.println("Starting Wi-Fi connection...");

  WiFi.mode(WIFI_STA);
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);

  lastWiFiAttemptMs = millis();
}

void handleWiFiConnection() {
  if (WiFi.status() == WL_CONNECTED) {
    if (!wifiWasConnected) {
      wifiWasConnected = true;

      Serial.print("Wi-Fi connected. IP: ");
      Serial.println(WiFi.localIP());
    }

    return;
  }

  if (wifiWasConnected) {
    wifiWasConnected = false;
    mqttWasConnected = false;

    Serial.println("Wi-Fi disconnected.");
  }

  if (millis() - lastWiFiAttemptMs < wifiRetryIntervalMs) {
    return;
  }

  Serial.println("Retrying Wi-Fi connection...");

  WiFi.disconnect(false);
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);

  lastWiFiAttemptMs = millis();
}

void toggleManualOverride() {
  settingsDraft.manualOverride = !settingsDraft.manualOverride;
  drawSettingsScreen();
}

void toggleManualMode() {
  if (settingsDraft.manualMode.equalsIgnoreCase("Off")) {
    settingsDraft.manualMode = "Fan";
  } else {
    settingsDraft.manualMode = "Off";
  }

  drawSettingsScreen();
}

void handleWindowBreezeTouch() {
  if (isWindowModeActive()) {
    Serial.println("Window mode active. Turning off manual override.");

    windowToggle = false;
    pendingWindowManualOn = false;
    pendingWindowManualOff = true;
    pendingWindowManualStartedMs = millis();

    applyManualOverrideLocally(false, "Off");
    publishWindowManualSettings(false, "Off");

    drawThermostatScreen();
    return;
  }

  if (isWindowPromptActive()) {
    Serial.println("Window prompt active. Turning on manual fan.");

    windowToggle = true;
    pendingWindowManualOn = true;
    pendingWindowManualOff = false;
    pendingWindowManualStartedMs = millis();

    applyManualOverrideLocally(true, "Fan");
    publishWindowManualSettings(true, "Fan");

    drawThermostatScreen();
    return;
  }

  Serial.println("Window icon touched, but window conditions are not active.");
}

void publishWindowManualSettings(bool manualOverride, const String& manualMode) {
  copySettingsDraftFromLatestStatus();

  settingsDraft.manualOverride = manualOverride;
  settingsDraft.manualMode = manualMode;

  publishSettingsCommand();
}

void publishModeCommand(const String& mode) {
  if (!mqttClient.connected()) {
    Serial.println("Skipping mode publish because MQTT is not connected.");
    return;
  }

  StaticJsonDocument<128> doc;
  doc["mode"] = mode;

  char buffer[128];
  size_t length = serializeJson(doc, buffer);

  bool published = mqttClient.publish(
    MODE_SET_TOPIC,
    buffer,
    length);

  Serial.print("Published mode command: ");
  Serial.println(published ? "yes" : "no");
  Serial.println(buffer);
}

void publishSettingsCommand() {
  if (!mqttClient.connected()) {
    Serial.println("Skipping settings publish because MQTT is not connected.");
    return;
  }

  StaticJsonDocument<384> doc;

  doc["heatSetPointDay"] = roundToTenths(settingsDraft.heatSetPointDay);
  doc["heatSetPointNight"] = roundToTenths(settingsDraft.heatSetPointNight);

  doc["humidityTargetFair"] = roundToTenths(settingsDraft.humidityTargetFair);
  doc["humidityTargetGood"] = roundToTenths(settingsDraft.humidityTargetGood);
  doc["humidityTargetIdeal"] = roundToTenths(settingsDraft.humidityTargetIdeal);

  doc["manualOverride"] = settingsDraft.manualOverride;
  doc["manualMode"] = settingsDraft.manualMode;

  char buffer[384];
  size_t length = serializeJson(doc, buffer);

  bool published = mqttClient.publish(
    SETTINGS_SET_TOPIC,
    buffer,
    length);

  Serial.print("Published settings command: ");
  Serial.println(published ? "yes" : "no");
  Serial.println(buffer);
}

bool hasPendingModeChange() {
  if (pendingMode == "") {
    return false;
  }

  if (millis() - pendingModeStartedMs > pendingModeHoldMs) {
    pendingMode = "";
    return false;
  }

  return true;
}

void publishUpstairsSensorReading() {
  bool hasReading = readSht45();

  if (!hasReading) {
    Serial.println("Skipping upstairs sensor publish because SHT45 read failed.");
    return;
  }
  if (!mqttClient.connected()) {
    Serial.println("Skipping upstairs sensor publish because MQTT is not connected.");
    return;
  }

  StaticJsonDocument<256> doc;

  doc["temperatureFahr"] = round(upstairsTemperatureF * 10.0) / 10.0;
  doc["relativeHumidity"] = round(upstairsRelativeHumidity * 10.0) / 10.0;
  doc["motionDetected"] = motionDetected;

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

void onMqttMessage(char* topic, byte* payload, unsigned int length) {
  Serial.print("MQTT message on topic: ");
  Serial.println(topic);

  StaticJsonDocument<2048> doc;

  DeserializationError error = deserializeJson(doc, payload, length);

  if (error) {
    Serial.print("Failed to parse status JSON: ");
    Serial.println(error.c_str());
    return;
  }

  latestStatus.heat = doc["heat"] | false;
  latestStatus.cool = doc["cool"] | false;
  latestStatus.fan = doc["fan"] | false;

  latestStatus.reason = doc["reason"] | "";
  latestStatus.mode = doc["mode"] | "";
  latestStatus.now = doc["now"] | "";

  latestStatus.heatSetPointFahr = doc["heatSetPointFahr"] | NAN;
  latestStatus.heatSetPointDay = doc["heatSetPointDay"] | NAN;
  latestStatus.heatSetPointNight = doc["heatSetPointNight"] | NAN;
  latestStatus.maxAbsHumSetPoint = doc["maxAbsHumSetPoint"] | NAN;

  latestStatus.humidityTargetIdeal = doc["humidityTargetIdeal"] | NAN;
  latestStatus.humidityTargetGood = doc["humidityTargetGood"] | NAN;
  latestStatus.humidityTargetFair = doc["humidityTargetFair"] | NAN;

  latestStatus.upTempCalibration = doc["upTempCalibration"] | NAN;
  latestStatus.upRelHumCalibration = doc["upRelHumCalibration"] | NAN;
  latestStatus.downTempCalibration = doc["downTempCalibration"] | NAN;
  latestStatus.downRelHumCalibration = doc["downRelHumCalibration"] | NAN;

  latestStatus.manualOverride = doc["manualOverride"] | false;
  latestStatus.manualMode = doc["manualMode"] | "Off";

  latestStatus.upstairsTemperature = doc["upstairsTemperature"] | NAN;
  latestStatus.upstairsRelativeHumidity = doc["upstairsRelativeHumidity"] | NAN;
  latestStatus.upstairsAbsoluteHumidity = doc["upstairsAbsoluteHumidity"] | NAN;

  latestStatus.downstairsTemperature = doc["downstairsTemperature"] | NAN;
  latestStatus.downstairsRelativeHumidity = doc["downstairsRelativeHumidity"] | NAN;
  latestStatus.downstairsAbsoluteHumidity = doc["downstairsAbsoluteHumidity"] | NAN;

  latestStatus.outsideTemperature = doc["outsideTemperature"] | NAN;
  latestStatus.outsideAbsoluteHumidity = doc["outsideAbsoluteHumidity"] | NAN;
  latestStatus.controlAbsoluteHumidity = doc["controlAbsoluteHumidity"] | NAN;

  latestStatus.coolHoursToday = doc["coolHoursToday"] | NAN;
  latestStatus.heatHoursToday = doc["heatHoursToday"] | NAN;

  latestStatus.lastHeatStarted = doc["lastHeatStarted"] | "";
  latestStatus.lastHeatStopped = doc["lastHeatStopped"] | "";
  latestStatus.lastCoolStarted = doc["lastCoolStarted"] | "";
  latestStatus.lastCoolStopped = doc["lastCoolStopped"] | "";

  shouldOpenWindows = calculateShouldOpenWindows();
  updateWindowToggleFromMqtt();
  bool modeChanged = updateSelectedModeFromStatus();

  Serial.print("Parsed HVAC status. statusMode=");
  Serial.print(latestStatus.mode);
  Serial.print(" selectedMode=");
  Serial.print(selectedMode);
  Serial.print(" pendingMode=");
  Serial.println(pendingMode);

  if (currentScreen == SCREEN_SETTINGS) {
    return;
  }

  if (currentScreen == SCREEN_STATS) {
    drawStatsScreen();
    return;
  }

  if (pendingMode != "") {
    bottomBarHeatW = getTargetHeatWidth();
    drawThermostatScreen();
    return;
  }

  drawThermostatScreen();

  if (modeChanged) {
    animateBottomBarToSelectedMode();
  }

}

void handleMqttConnection() {
  if (WiFi.status() != WL_CONNECTED) {
    return;
  }

  if (mqttClient.connected()) {
    if (!mqttWasConnected) {
      mqttWasConnected = true;
      Serial.println("MQTT connected.");
    }

    return;
  }

  if (mqttWasConnected) {
    mqttWasConnected = false;
    Serial.println("MQTT disconnected.");
  }

  if (millis() - lastMqttAttemptMs < mqttRetryIntervalMs) {
    return;
  }

  lastMqttAttemptMs = millis();

  Serial.print("Connecting to MQTT...");

  String clientId = "waveshare-hvac-display-";
  clientId += String((uint32_t)ESP.getEfuseMac(), HEX);

  if (mqttClient.connect(clientId.c_str())) {
    mqttWasConnected = true;

    Serial.println("connected");

    mqttClient.subscribe(STATUS_TOPIC);

    Serial.print("Subscribed to ");
    Serial.println(STATUS_TOPIC);

    mqttClient.publish("hvac/upstairs/boot", "waveshare booted", true);
  } else {
    Serial.print("failed, rc=");
    Serial.print(mqttClient.state());
    Serial.println(". Will retry later.");
  }
}

void scanMainI2CBus() {
  Serial.println("Scanning main I2C bus SDA=21 SCL=22");

  for (uint8_t address = 1; address < 127; address++) {
    Wire.beginTransmission(address);
    uint8_t error = Wire.endTransmission();

    if (error == 0) {
      Serial.print("Found I2C device at 0x");

      if (address < 16) {
        Serial.print("0");
      }

      Serial.println(address, HEX);
    }

    delay(2);
  }

  Serial.println("I2C scan complete.");
}

void scanI2CBus(TwoWire& bus, int sda, int scl, const char* name) {
  Serial.print("Scanning ");
  Serial.print(name);
  Serial.print(" SDA=");
  Serial.print(sda);
  Serial.print(" SCL=");
  Serial.println(scl);

  bus.begin(sda, scl);
  delay(100);

  bool foundAny = false;

  for (uint8_t address = 1; address < 127; address++) {
    bus.beginTransmission(address);
    uint8_t error = bus.endTransmission();

    if (error == 0) {
      foundAny = true;
      Serial.print("Found I2C device at 0x");

      if (address < 16) {
        Serial.print("0");
      }

      Serial.println(address, HEX);
    }
  }

  if (!foundAny) {
    Serial.println("No I2C devices found.");
  }

  Serial.println();
}

bool i2cAddressResponds(uint8_t address) {
  Wire.beginTransmission(address);
  uint8_t error = Wire.endTransmission();

  return error == 0;
}

bool sht45AddressResponds(uint8_t address) {
  sht45Wire.beginTransmission(address);
  uint8_t error = sht45Wire.endTransmission();

  return error == 0;
}

void initSht45() {
  Serial.println("Initializing upstairs SHT45 on separate I2C bus GPIO21/GPIO38...");

  sht45Wire.begin(SHT45_SDA, SHT45_SCL);
  sht45Wire.setTimeOut(100);
  delay(100);

  const uint8_t SHT45_ADDRESS = 0x44;

  if (!sht45AddressResponds(SHT45_ADDRESS)) {
    Serial.println("No SHT45 response at I2C address 0x44 on GPIO21/GPIO38.");
    sht45Ready = false;
    return;
  }

  Serial.println("SHT45 address responded. Starting library...");

  if (!sht45.begin(&sht45Wire)) {
    Serial.println("Could not start SHT45 library.");
    sht45Ready = false;
    return;
  }

  sht45.setPrecision(SHT4X_HIGH_PRECISION);
  sht45.setHeater(SHT4X_NO_HEATER);

  sht45Ready = true;
  Serial.println("SHT45 initialized.");
}

bool readSht45() {
  if (!sht45Ready) {
    return false;
  }

  sensors_event_t humidity;
  sensors_event_t temperature;

  if (!sht45.getEvent(&humidity, &temperature)) {
    Serial.println("Failed to read SHT45.");
    return false;
  }

  upstairsTemperatureF = temperature.temperature * 9.0 / 5.0 + 32;
  upstairsRelativeHumidity = humidity.relative_humidity;

  Serial.print("SHT45 temp F: ");
  Serial.println(upstairsTemperatureF, 1);

  Serial.print("SHT45 RH: ");
  Serial.println(upstairsRelativeHumidity, 1);

  return true;
}

void scanSht45Bus() {
  Serial.println("Scanning SHT45 I2C bus SDA=21 SCL=38");

  sht45Wire.begin(SHT45_SDA, SHT45_SCL);
  sht45Wire.setTimeOut(100);
  delay(100);

  for (uint8_t address = 1; address < 127; address++) {
    sht45Wire.beginTransmission(address);
    uint8_t error = sht45Wire.endTransmission();

    if (error == 0) {
      Serial.print("Found I2C device at 0x");

      if (address < 16) {
        Serial.print("0");
      }

      Serial.println(address, HEX);
    }

    delay(2);
  }

  Serial.println("SHT45 I2C scan complete.");
}

void initLd2410Out() {
  pinMode(LD2410_OUT_PIN, INPUT);
  Serial.println("LD2410 OUT pin started.");
}

void pollLd2410Out() {
  static bool lastMotionDetected = false;
  static unsigned long lastPrintMs = 0;

  motionDetected = digitalRead(LD2410_OUT_PIN) == HIGH;

  if (motionDetected != lastMotionDetected || millis() - lastPrintMs > 10000) {
    lastMotionDetected = motionDetected;
    lastPrintMs = millis();

    Serial.print("LD2410 presence OUT: ");
    Serial.println(motionDetected ? "yes" : "no");
  }
}

bool isValidScreenTouch(int x, int y) {
  return x >= 0 && x < SCREEN_W && y >= 0 && y < SCREEN_H;
}

bool isTouchInRect(int x, int y, int rectX, int rectY, int rectW, int rectH) {
  return x >= rectX &&
         x <= rectX + rectW &&
         y >= rectY &&
         y <= rectY + rectH;
}

bool isSettingsUpArrowTouch(int x, int y) {
  return isTouchInRect(
    x,
    y,
    SETTINGS_ARROW_X,
    SETTINGS_UP_ARROW_Y,
    SETTINGS_ARROW_W,
    SETTINGS_ARROW_H);
}

bool isSettingsDownArrowTouch(int x, int y) {
  return isTouchInRect(
    x,
    y,
    SETTINGS_ARROW_X,
    SETTINGS_DOWN_ARROW_Y,
    SETTINGS_ARROW_W,
    SETTINGS_ARROW_H);
}

bool isSettingsManualOverrideTouch(int x, int y) {
  return isTouchInRect(
    x,
    y,
    SETTINGS_MANUAL_OVERRIDE_X,
    SETTINGS_FOOTER_Y,
    SETTINGS_FOOTER_BUTTON_W,
    SETTINGS_FOOTER_H);
}

bool isSettingsManualModeTouch(int x, int y) {
  return isTouchInRect(
    x,
    y,
    SETTINGS_MANUAL_MODE_X,
    SETTINGS_FOOTER_Y,
    SETTINGS_FOOTER_BUTTON_W,
    SETTINGS_FOOTER_H);
}

bool isSettingsValueBoxTouch(int x, int y, int rowY) {
  return isTouchInRect(
    x,
    y,
    SETTINGS_VALUE_X,
    rowY,
    SETTINGS_VALUE_W,
    SETTINGS_VALUE_H);
}

SettingTarget getSettingsTargetAtTouch(int x, int y) {
  int rowY = SETTINGS_ROW_START_Y;

  if (isSettingsValueBoxTouch(x, y, rowY)) {
    return SETTING_TARGET_FAIR_HUMIDITY;
  }

  if (isSettingsValueBoxTouch(x, y, rowY + SETTINGS_ROW_GAP)) {
    return SETTING_TARGET_GOOD_HUMIDITY;
  }

  if (isSettingsValueBoxTouch(x, y, rowY + (SETTINGS_ROW_GAP * 2))) {
    return SETTING_TARGET_IDEAL_HUMIDITY;
  }

  if (isSettingsValueBoxTouch(x, y, rowY + (SETTINGS_ROW_GAP * 3))) {
    return SETTING_TARGET_DAY_HEATING;
  }

  if (isSettingsValueBoxTouch(x, y, rowY + (SETTINGS_ROW_GAP * 4))) {
    return SETTING_TARGET_NIGHT_HEATING;
  }

  return SETTING_TARGET_NONE;
}

bool isStatsTouch(int x, int y) {
  int half = TOP_ICON_TOUCH_SIZE / 2;

  return x >= STATS_CENTER_X - half &&
         x <= STATS_CENTER_X + half &&
         y >= TOP_ICON_CENTER_Y - half &&
         y <= TOP_ICON_CENTER_Y + half;
}

bool isWindowBreezeTouch(int x, int y) {
  int half = TOP_ICON_TOUCH_SIZE / 2;

  return x >= WINDOW_BREEZE_CENTER_X - half &&
         x <= WINDOW_BREEZE_CENTER_X + half &&
         y >= TOP_ICON_CENTER_Y - half &&
         y <= TOP_ICON_CENTER_Y + half;
}

bool isGearTouch(int x, int y) {
  int half = GEAR_TOUCH_SIZE / 2;

  return x >= GEAR_CENTER_X - half &&
         x <= GEAR_CENTER_X + half &&
         y >= GEAR_CENTER_Y - half &&
         y <= GEAR_CENTER_Y + half;
}

bool isSettingsExitTouch(int x, int y) {
  return x >= SETTINGS_EXIT_X &&
         x <= SETTINGS_EXIT_X + SETTINGS_EXIT_W &&
         y >= SETTINGS_EXIT_Y &&
         y <= SETTINGS_EXIT_Y + SETTINGS_EXIT_H;
}

void changeSelectedMode(const String& newMode) {
  if (selectedMode == newMode) {
    return;
  }

  selectedMode = newMode;
  pendingMode = newMode;
  pendingModeStartedMs = millis();

  int startHeatW = bottomBarHeatW;
  int targetHeatW = getTargetHeatWidth();

  for (int step = 1; step <= MODE_ANIMATION_STEPS; step++) {
    float progress = (float)step / (float)MODE_ANIMATION_STEPS;
    progress = progress * progress * (3.0 - 2.0 * progress);

    bottomBarHeatW = startHeatW + (int)((targetHeatW - startHeatW) * progress);

    drawBottomBar();
    gfx->flush();

    delay(MODE_ANIMATION_DELAY_MS);
  }

  bottomBarHeatW = targetHeatW;

  drawBottomBar();
  gfx->flush();

  publishModeCommand(selectedMode);
}

void handleTouchPress(int x, int y) {
  if (currentScreen == SCREEN_MAIN && isStatsTouch(x, y)) {
    Serial.println("Stats icon touched.");

    currentScreen = SCREEN_STATS;
    drawStatsScreen();

    return;
  }

  if (currentScreen == SCREEN_MAIN && isWindowBreezeTouch(x, y)) {
    Serial.println("Window breeze icon touched.");
    handleWindowBreezeTouch();
    return;
  }

  if (currentScreen == SCREEN_MAIN && isGearTouch(x, y)) {
    Serial.println("Gear/settings touched.");

    copySettingsDraftFromLatestStatus();

    currentSettingTarget = SETTING_TARGET_NONE;
    settingsFlashOn = true;
    lastSettingsFlashMs = millis();

    currentScreen = SCREEN_SETTINGS;
    drawSettingsScreen();

    return;
  }

  if (currentScreen == SCREEN_SETTINGS) {
    if (isSettingsExitTouch(x, y)) {
      Serial.println("Settings exit touched.");

      publishSettingsCommand();

      settingsDraftActive = false;
      currentSettingTarget = SETTING_TARGET_NONE;

      currentScreen = SCREEN_MAIN;
      drawThermostatScreen();

      return;
    }

    if (isSettingsManualOverrideTouch(x, y)) {
      Serial.println("Manual override touched.");
      toggleManualOverride();
      return;
    }

    if (isSettingsManualModeTouch(x, y)) {
      Serial.println("Manual mode touched.");
      toggleManualMode();
      return;
    }

    if (isSettingsUpArrowTouch(x, y)) {
      Serial.println("Settings up arrow touched.");
      adjustCurrentSettingTarget(0.1);
      return;
    }

    if (isSettingsDownArrowTouch(x, y)) {
      Serial.println("Settings down arrow touched.");
      adjustCurrentSettingTarget(-0.1);
      return;
    }

    SettingTarget touchedTarget = getSettingsTargetAtTouch(x, y);

    if (touchedTarget != SETTING_TARGET_NONE) {
      Serial.println("Settings value touched.");

      currentSettingTarget = touchedTarget;
      settingsFlashOn = true;
      lastSettingsFlashMs = millis();

      drawSettingsScreen();

      return;
    }

    Serial.println("Settings screen touched.");
    return;
  }

  if (currentScreen == SCREEN_STATS) {
    if (isSettingsExitTouch(x, y)) {
      Serial.println("Stats exit touched.");

      currentScreen = SCREEN_MAIN;
      drawThermostatScreen();

      return;
    }

    Serial.println("Stats screen touched.");
    return;
  }

  int barY = SAFE_Y + SAFE_H - BOTTOM_BAR_H;
  int barBottom = SAFE_Y + SAFE_H - 1;

  bool inBottomBar =
    x >= SAFE_X && x < SAFE_X + SAFE_W && y >= barY && y <= barBottom;

  if (!inBottomBar) {
    return;
  }

  int heatW = bottomBarHeatW;
  int dividerX = SAFE_X + heatW;

  if (x < dividerX) {
    Serial.println("Touch selected Heat");
    changeSelectedMode("Heat");
    return;
  }

  Serial.println("Touch selected Cool");
  changeSelectedMode("Cool");
}

void pollTouch() {
  if (!touchReady) {
    return;
  }

  static bool touchWasDown = false;
  static unsigned long lastTouchSeenMs = 0;

  const unsigned long touchReleaseDebounceMs = 250;

  bsp_touch_read();

  touch_data_t touchData;

  bool hasTouch = bsp_touch_get_coordinates(&touchData);

  if (!hasTouch) {
    if (touchWasDown &&
        millis() - lastTouchSeenMs >= touchReleaseDebounceMs) {
      touchWasDown = false;
    }

    return;
  }

  lastTouchSeenMs = millis();

  int rawX = touchData.coords[0].x;
  int rawY = touchData.coords[0].y;

  int x = rawY;
  int y = 320 - rawX;

  if (!isValidScreenTouch(x, y)) {
    Serial.print("Ignored invalid touch X=");
    Serial.print(x);
    Serial.print(" Y=");
    Serial.println(y);
    return;
  }

  if (touchWasDown) {
    return;
  }

  touchWasDown = true;

  handleTouchPress(x, y);
}

void setup() {
  Serial.begin(115200);
  delay(1000);

  Serial.print("Free heap: ");
  Serial.println(ESP.getFreeHeap());

  Serial.print("PSRAM size: ");
  Serial.println(ESP.getPsramSize());

  Serial.print("Free PSRAM: ");
  Serial.println(ESP.getFreePsram());

  initDisplay();
  initLd2410Out();

  bottomBarHeatW = getTargetHeatWidth();
  drawThermostatScreen();

  scanSht45Bus();
  initSht45();
  readSht45();

  mqttClient.setBufferSize(2048);
  mqttClient.setServer(MQTT_HOST, MQTT_PORT);
  mqttClient.setCallback(onMqttMessage);

  startWiFiConnection();
}

void loop() {
  pollTouch();

  if (mqttClient.connected()) {
    mqttClient.loop();
  }

  pollLd2410Out();

  handleWiFiConnection();
  handleMqttConnection();

  unsigned long now = millis();

  if (now - lastPublishMs >= publishIntervalMs) {
    lastPublishMs = now;
    publishUpstairsSensorReading();
  }

  updateSettingsFlash();
  updateWindSound();
}
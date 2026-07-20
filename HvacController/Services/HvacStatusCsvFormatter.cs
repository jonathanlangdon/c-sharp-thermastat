using System.Globalization;
using HvacController.Models;

namespace HvacController.Services;

public static class HvacStatusCsvFormatter
{
    public const string Header =
        "receivedAt,now,lastSensorUpdate,lastMotionDetected,outsideWeatherUpdatedAt," +
        "heat,cool,fan,reason,mode," +
        "heatSetPointFahr,heatSetPointDay,heatSetPointNight,maxAbsHumSetPoint," +
        "upTempCalibration,upRelHumCalibration,downTempCalibration,downRelHumCalibration," +
        "upstairsTemperature,upstairsRelativeHumidity,upstairsAbsoluteHumidity," +
        "downstairsTemperature,downstairsRelativeHumidity,downstairsAbsoluteHumidity," +
        "controlAbsoluteHumidity,outsideTemperature,outsideAbsoluteHumidity," +
        "wasHeating,wasCooling,lastHeatStarted,lastHeatStopped,lastCoolStarted,lastCoolStopped,coolHoursToday,heatHoursToday";

    public static string Format(
        DateTimeOffset timestamp,
        ThermostatStatusMessage message)
    {
        return string.Join(
            ",",
            FormatDateTime(timestamp),
            FormatDateTime(message.Now),
            FormatDateTime(message.LastSensorUpdate),
            FormatDateTime(message.LastMotionDetected),
            FormatDateTime(message.OutsideWeatherUpdatedAt),

            FormatBool(message.Heat),
            FormatBool(message.Cool),
            FormatBool(message.Fan),
            Escape(message.Reason),
            message.Mode.ToString(),

            FormatDouble(message.HeatSetPointFahr, "0.0"),
            FormatDouble(message.HeatSetPointDay, "0.0"),
            FormatDouble(message.HeatSetPointNight, "0.0"),
            FormatDouble(message.MaxAbsHumSetPoint, "0.00"),

            FormatDouble(message.UpTempCalibration, "0.0"),
            FormatDouble(message.UpRelHumCalibration, "0.0"),
            FormatDouble(message.DownTempCalibration, "0.0"),
            FormatDouble(message.DownRelHumCalibration, "0.0"),

            FormatDouble(message.UpstairsTemperature, "0.0"),
            FormatDouble(message.UpstairsRelativeHumidity, "0.0"),
            FormatDouble(message.UpstairsAbsoluteHumidity, "0.00"),

            FormatDouble(message.DownstairsTemperature, "0.0"),
            FormatDouble(message.DownstairsRelativeHumidity, "0.0"),
            FormatDouble(message.DownstairsAbsoluteHumidity, "0.00"),

            FormatDouble(message.ControlAbsoluteHumidity, "0.00"),
            FormatDouble(message.OutsideTemperature, "0.0"),
            FormatDouble(message.OutsideAbsoluteHumidity, "0.00"),

            FormatBool(message.WasHeating),
            FormatBool(message.WasCooling),
            FormatDateTime(message.LastHeatStarted),
            FormatDateTime(message.LastHeatStopped),
            FormatDateTime(message.LastCoolStarted),
            FormatDateTime(message.LastCoolStopped),
            FormatDouble(message.CoolHoursToday, "0.00"),
            FormatDouble(message.HeatHoursToday, "0.00"));
    }

    private static string FormatBool(bool value)
    {
        return value ? "true" : "false";
    }

    private static string FormatDouble(double? value, string format)
    {
        return value is null
            ? ""
            : value.Value.ToString(format, CultureInfo.InvariantCulture);
    }

    private static string FormatDateTime(DateTimeOffset value)
    {
        return value.ToString("O", CultureInfo.InvariantCulture);
    }

    private static string FormatDateTime(DateTimeOffset? value)
    {
        return value is null
            ? ""
            : value.Value.ToString("O", CultureInfo.InvariantCulture);
    }

    private static string Escape(string? value)
    {
        value ??= "";

        if (!value.Contains(',') &&
            !value.Contains('"') &&
            !value.Contains('\n') &&
            !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
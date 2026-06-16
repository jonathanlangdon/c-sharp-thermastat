using System.Globalization;
using HvacController.Models;

namespace HvacController.Services;

public static class HvacStatusCsvFormatter
{
    public const string Header =
        "timestamp,heat,cool,fan,mode,upstairsTemperature,upstairsAbsoluteHumidity,downstairsTemperature,downstairsAbsoluteHumidity,outsideTemperature,outsideAbsoluteHumidity,reason";

    public static string Format(
        DateTimeOffset timestamp,
        ThermostatStatusMessage message)
    {
        return string.Join(
            ",",
            timestamp.ToString("O", CultureInfo.InvariantCulture),
            FormatBool(message.Heat),
            FormatBool(message.Cool),
            FormatBool(message.Fan),
            message.Mode.ToString(),
            FormatDouble(message.UpstairsTemperature, "0.0"),
            FormatDouble(message.UpstairsAbsoluteHumidity, "0.00"),
            FormatDouble(message.DownstairsTemperature, "0.0"),
            FormatDouble(message.DownstairsAbsoluteHumidity, "0.00"),
            FormatDouble(message.OutsideTemperature, "0.0"),
            FormatDouble(message.OutsideAbsoluteHumidity, "0.00"),
            Escape(message.Reason));
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

    private static string Escape(string value)
    {
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


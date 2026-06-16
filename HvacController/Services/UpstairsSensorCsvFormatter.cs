using System.Globalization;
using HvacController.Models;

namespace HvacController.Services;

public static class UpstairsSensorCsvFormatter
{
    public const string Header =
        "timestamp,temperatureFahr,relativeHumidity,motionDetected,mode";

    public static string Format(
        DateTimeOffset timestamp,
        UpstairsSensorMessage message)
    {
        var motionDetected = message.MotionDetected is null
            ? ""
            : message.MotionDetected.Value
                ? "true"
                : "false";

        var mode = message.Mode?.ToString() ?? "";

        return string.Join(
            ",",
            timestamp.ToString("O", CultureInfo.InvariantCulture),
            message.TemperatureFahr.ToString("0.0", CultureInfo.InvariantCulture),
            message.RelativeHumidity.ToString("0.0", CultureInfo.InvariantCulture),
            motionDetected,
            mode);
    }
}

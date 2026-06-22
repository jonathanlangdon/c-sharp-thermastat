using HvacController.Models;
using HvacController.Services;

namespace HvacController.Tests;

public sealed class ThermostatEngineHeatSetPointTests
{
    [Fact]
    public void Evaluate_IncludesDayHeatSetPointWhenMotionWasRecentlyDetectedDuringDay()
    {
        var now = new DateTimeOffset(
            2026, 6, 16, 14, 0, 0,
            TimeSpan.FromHours(-4));

        var engine = new ThermostatEngine(new HvacSettings());

        var input = HeatInput(now) with
        {
            CurrentTempFahrUp = 72,
            RelHumidityUpstairs = 50,
            LastMotionDetected = now,

            DayHeatSetPoint = 70.5,
            NightHeatSetPoint = 65.0
        };

        var (output, _) = engine.Evaluate(
            input,
            ThermostatRuntimeState.Empty);

        Assert.Equal(70.5, output.HeatSetPointFahr!.Value, 1);
    }

    [Fact]
    public void Evaluate_IncludesNightHeatSetPointAtNight()
    {
        var now = new DateTimeOffset(
            2026, 6, 16, 22, 0, 0,
            TimeSpan.FromHours(-4));

        var engine = new ThermostatEngine(new HvacSettings());

        var input = HeatInput(now) with
        {
            CurrentTempFahrUp = 72,
            RelHumidityUpstairs = 50,
            LastMotionDetected = now,

            DayHeatSetPoint = 70.5,
            NightHeatSetPoint = 65.0
        };

        var (output, _) = engine.Evaluate(
            input,
            ThermostatRuntimeState.Empty);

        Assert.Equal(65.0, output.HeatSetPointFahr!.Value, 1);
    }

    [Fact]
    public void Evaluate_UsesAlreadyCalibratedTemperatureForHeatDecision()
    {
        var now = new DateTimeOffset(
            2026, 6, 16, 14, 0, 0,
            TimeSpan.FromHours(-4));

        var engine = new ThermostatEngine(new HvacSettings());

        var input = HeatInput(now) with
        {
            // This represents the already-calibrated temperature.
            CurrentTempFahrUp = 69.4,
            RelHumidityUpstairs = 50,
            LastMotionDetected = now,

            DayHeatSetPoint = 70.5,
            NightHeatSetPoint = 65.0
        };

        var (output, _) = engine.Evaluate(
            input,
            ThermostatRuntimeState.Empty);

        Assert.True(output.Heat);
        Assert.False(output.Cool);
        Assert.True(output.Fan);
        Assert.Equal(70.5, output.HeatSetPointFahr!.Value, 1);
    }

    private static ThermostatInput HeatInput(DateTimeOffset now)
    {
        return new ThermostatInput
        {
            Mode = HvacMode.Heat,
            Now = now,
            LastSensorUpdate = now,

            DayHeatSetPoint = 70.5,
            NightHeatSetPoint = 65.0,
            AbsoluteHumidityCoolingOnThreshold = 10.8,

            UpTempCalibration = 0.0,
            UpRelHumCalibration = 0.0,
            DownTempCalibration = 0.0,
            DownRelHumCalibration = 0.0
        };
    }
}
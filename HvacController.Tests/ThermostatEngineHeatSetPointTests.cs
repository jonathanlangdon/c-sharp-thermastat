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

        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 72,
            HumidityUpstairs = 50,
            Mode = HvacMode.Heat,
            Now = now,
            LastSensorUpdate = now,
            LastMotionDetected = now
        };

        var (output, _) = engine.Evaluate(
            input,
            ThermostatRuntimeState.Empty);

        Assert.Equal(70.6, output.HeatSetPointFahr!.Value, 1);
    }

    [Fact]
    public void Evaluate_IncludesNightHeatSetPointAtNight()
    {
        var now = new DateTimeOffset(
            2026, 6, 16, 22, 0, 0,
            TimeSpan.FromHours(-4));

        var engine = new ThermostatEngine(new HvacSettings());

        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 72,
            HumidityUpstairs = 50,
            Mode = HvacMode.Heat,
            Now = now,
            LastSensorUpdate = now,
            LastMotionDetected = now
        };

        var (output, _) = engine.Evaluate(
            input,
            ThermostatRuntimeState.Empty);

        Assert.Equal(65.0, output.HeatSetPointFahr!.Value, 1);
    }
}

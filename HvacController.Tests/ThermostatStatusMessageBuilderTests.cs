using HvacController.Models;
using HvacController.Services;

namespace HvacController.Tests;

public sealed class ThermostatStatusMessageBuilderTests
{
    [Fact]
    public void Build_CopiesThermostatOutput()
    {
        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 72.1,
            HumidityUpstairs = 50,
            Mode = HvacMode.Cool,
            Now = DateTimeOffset.Parse("2026-06-12T12:00:00Z"),
            LastSensorUpdate = DateTimeOffset.Parse("2026-06-12T12:00:00Z")
        };

        var output = new ThermostatOutput
        {
            Heat = false,
            Cool = true,
            Fan = true,
            Reason = "Everything Normal"
        };

        var message = ThermostatStatusMessageBuilder.Build(
            input,
            output,
            ThermostatRuntimeState.Empty);

        Assert.False(message.Heat);
        Assert.True(message.Cool);
        Assert.True(message.Fan);
        Assert.Equal("Everything Normal", message.Reason);
        Assert.Equal(HvacMode.Cool, message.Mode);
    }

    [Fact]
    public void Build_IncludesUpstairsHumidityValues()
    {
        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 72.0,
            HumidityUpstairs = 50.0,
            Mode = HvacMode.Cool,
            Now = DateTimeOffset.Parse("2026-06-12T12:00:00Z"),
            LastSensorUpdate = DateTimeOffset.Parse("2026-06-12T12:00:00Z")
        };

        var output = new ThermostatOutput();

        var message = ThermostatStatusMessageBuilder.Build(
            input,
            output,
            ThermostatRuntimeState.Empty);

        Assert.Equal(72.0, message.UpstairsTemperature);
        Assert.Equal(50.0, message.UpstairsRelativeHumidity);
        Assert.Equal(9.83, message.UpstairsAbsoluteHumidity);
    }

    [Fact]
    public void Build_IncludesDownstairsHumidityValues()
    {
        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 72.0,
            HumidityUpstairs = 40.0,
            CurrentTempFahrDown = 66.4,
            HumidityDownstairs = 55.2,
            Mode = HvacMode.Cool,
            Now = DateTimeOffset.Parse("2026-06-12T12:00:00Z"),
            LastSensorUpdate = DateTimeOffset.Parse("2026-06-12T12:00:00Z")
        };

        var output = new ThermostatOutput();

        var message = ThermostatStatusMessageBuilder.Build(
            input,
            output,
            ThermostatRuntimeState.Empty);

        Assert.Equal(66.4, message.DownstairsTemperature);
        Assert.Equal(55.2, message.DownstairsRelativeHumidity);
        Assert.NotNull(message.DownstairsAbsoluteHumidity);
    }

    [Fact]
    public void Build_ControlAbsoluteHumidityUsesMaximumIndoorAbsoluteHumidity()
    {
        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 72.0,
            HumidityUpstairs = 50.0,
            CurrentTempFahrDown = 66.4,
            HumidityDownstairs = 55.2,
            Mode = HvacMode.Cool,
            Now = DateTimeOffset.Parse("2026-06-12T12:00:00Z"),
            LastSensorUpdate = DateTimeOffset.Parse("2026-06-12T12:00:00Z")
        };

        var output = new ThermostatOutput();

        var message = ThermostatStatusMessageBuilder.Build(
            input,
            output,
            ThermostatRuntimeState.Empty);

        Assert.Equal(input.ControlHumidity, message.ControlAbsoluteHumidity);
    }

    [Fact]
    public void Build_IncludesOutsideWeatherValues()
    {
        var input = new ThermostatInput
        {
            CurrentTempFahrUp = 72.0,
            HumidityUpstairs = 50.0,
            OutsideAbsoluteHumidity = 9.4,
            Mode = HvacMode.Cool,
            Now = DateTimeOffset.Parse("2026-06-12T12:00:00Z"),
            LastSensorUpdate = DateTimeOffset.Parse("2026-06-12T12:00:00Z")
        };

        var output = new ThermostatOutput();

        var message = ThermostatStatusMessageBuilder.Build(
            input,
            output,
            ThermostatRuntimeState.Empty,
            outsideTemperature: 70.0);

        Assert.Equal(70.0, message.OutsideTemperature);
        Assert.Equal(9.4, message.OutsideAbsoluteHumidity);
    }
}

using HvacController.Models;

namespace HvacController.Tests;

public sealed class HvacSettingsTests
{
    [Fact]
    public void Constructor_WhenMinimumRunTimeIsZero_Throws()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new HvacSettings
            {
                MinimumRunTime = TimeSpan.Zero
            }.ValidateFixedSettings());

        Assert.Equal("MinimumRunTime", exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenMinimumOffTimeIsZero_Throws()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new HvacSettings
            {
                MinimumOffTime = TimeSpan.Zero
            }.ValidateFixedSettings());

        Assert.Equal("MinimumOffTime", exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenSensorTimeoutIsZero_Throws()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new HvacSettings
            {
                SensorTimeout = TimeSpan.Zero
            }.ValidateFixedSettings());

        Assert.Equal("SensorTimeout", exception.ParamName);
    }

    [Fact]
    public void UserCoolingThresholds_WhenOnThresholdIsLessThanOffThreshold_IsInvalid()
    {
        var settings = new HvacSettings
        {
            AbsoluteHumidityCoolingOnThreshold = 8.5,
            AbsoluteHumidityCoolingOffThreshold = 9.0
        };

        Assert.False(settings.HasValidCoolingThresholds());
    }

    [Fact]
    public void UserCoolingThresholds_WhenOnThresholdEqualsOffThreshold_IsInvalid()
    {
        var settings = new HvacSettings
        {
            AbsoluteHumidityCoolingOnThreshold = 9.0,
            AbsoluteHumidityCoolingOffThreshold = 9.0
        };

        Assert.False(settings.HasValidCoolingThresholds());
    }

    [Fact]
    public void UserCoolingThresholds_WhenOnThresholdIsGreaterThanOffThreshold_IsValid()
    {
        var settings = new HvacSettings
        {
            AbsoluteHumidityCoolingOnThreshold = 9.5,
            AbsoluteHumidityCoolingOffThreshold = 9.0
        };

        Assert.True(settings.HasValidCoolingThresholds());
    }
}
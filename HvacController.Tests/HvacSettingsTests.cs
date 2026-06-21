using HvacController.Models;

namespace HvacController.Tests;

public sealed class HvacSettingsTests
{

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

}
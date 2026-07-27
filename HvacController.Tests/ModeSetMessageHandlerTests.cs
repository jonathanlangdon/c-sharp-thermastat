using HvacController.Models;
using HvacController.Services;

namespace HvacController.Tests;

public sealed class ModeSetMessageHandlerTests
{
    [Fact]
    public void Handle_WhenPayloadSetsHeat_UpdatesPersistentModeToHeat()
    {
        var store = new FakeThermostatPersistentStateStore
        {
            State = new ThermostatPersistentState
            {
                Mode = HvacMode.Cool,
                HeatSetPointDay = 71.5,
                HeatSetPointNight = 64.5,
                HumidityTargetIdeal = 8.8,
                HumidityTargetGood = 10.4,
                HumidityTargetFair = 11.2,
                WasCooling = true
            }
        };

        var handler = new ModeSetMessageHandler(store);

        var handled = handler.Handle("""{"mode":"Heat"}""");

        Assert.True(handled);
        Assert.Equal(HvacMode.Heat, store.State.Mode);

        Assert.Equal(71.5, store.State.HeatSetPointDay);
        Assert.Equal(64.5, store.State.HeatSetPointNight);
        Assert.Equal(8.8, store.State.HumidityTargetIdeal);
        Assert.Equal(10.4, store.State.HumidityTargetGood);
        Assert.Equal(11.2, store.State.HumidityTargetFair);
        Assert.True(store.State.WasCooling);
    }

    [Fact]
    public void Handle_WhenPayloadSetsCool_UpdatesPersistentModeToCool()
    {
        var store = new FakeThermostatPersistentStateStore
        {
            State = new ThermostatPersistentState
            {
                Mode = HvacMode.Heat,
                HeatSetPointDay = 70.5,
                HeatSetPointNight = 65.0,
                HumidityTargetIdeal = 9.0,
                HumidityTargetGood = 10.0,
                HumidityTargetFair = 11.0
            }
        };

        var handler = new ModeSetMessageHandler(store);

        var handled = handler.Handle("""{"mode":"Cool"}""");

        Assert.True(handled);
        Assert.Equal(HvacMode.Cool, store.State.Mode);
    }

    [Fact]
    public void Handle_WhenPayloadUsesLowercaseMode_UpdatesPersistentMode()
    {
        var store = new FakeThermostatPersistentStateStore
        {
            State = new ThermostatPersistentState
            {
                Mode = HvacMode.Heat
            }
        };

        var handler = new ModeSetMessageHandler(store);

        var handled = handler.Handle("""{"mode":"cool"}""");

        Assert.True(handled);
        Assert.Equal(HvacMode.Cool, store.State.Mode);
    }

    [Fact]
    public void Handle_WhenPayloadHasExtraWhitespace_UpdatesPersistentMode()
    {
        var store = new FakeThermostatPersistentStateStore
        {
            State = new ThermostatPersistentState
            {
                Mode = HvacMode.Cool
            }
        };

        var handler = new ModeSetMessageHandler(store);

        var handled = handler.Handle("""{"mode":"  Heat  "}""");

        Assert.True(handled);
        Assert.Equal(HvacMode.Heat, store.State.Mode);
    }

    [Fact]
    public void Handle_WhenPayloadHasInvalidMode_ReturnsFalseAndDoesNotChangeState()
    {
        var store = new FakeThermostatPersistentStateStore
        {
            State = new ThermostatPersistentState
            {
                Mode = HvacMode.Heat,
                HeatSetPointDay = 71.0,
                HeatSetPointNight = 66.0,
                HumidityTargetIdeal = 8.5,
                HumidityTargetGood = 10.2,
                HumidityTargetFair = 11.5
            }
        };

        var handler = new ModeSetMessageHandler(store);

        var handled = handler.Handle("""{"mode":"Auto"}""");

        Assert.False(handled);
        Assert.Equal(HvacMode.Heat, store.State.Mode);
        Assert.Equal(71.0, store.State.HeatSetPointDay);
        Assert.Equal(66.0, store.State.HeatSetPointNight);
        Assert.Equal(8.5, store.State.HumidityTargetIdeal);
        Assert.Equal(10.2, store.State.HumidityTargetGood);
        Assert.Equal(11.5, store.State.HumidityTargetFair);
    }

    [Fact]
    public void Handle_WhenPayloadIsMissingMode_ReturnsFalseAndDoesNotChangeState()
    {
        var store = new FakeThermostatPersistentStateStore
        {
            State = new ThermostatPersistentState
            {
                Mode = HvacMode.Cool
            }
        };

        var handler = new ModeSetMessageHandler(store);

        var handled = handler.Handle("""{"temperatureFahr":67}""");

        Assert.False(handled);
        Assert.Equal(HvacMode.Cool, store.State.Mode);
    }

    [Fact]
    public void Handle_WhenPayloadIsInvalidJson_ReturnsFalseAndDoesNotChangeState()
    {
        var store = new FakeThermostatPersistentStateStore
        {
            State = new ThermostatPersistentState
            {
                Mode = HvacMode.Cool
            }
        };

        var handler = new ModeSetMessageHandler(store);

        var handled = handler.Handle("not valid json");

        Assert.False(handled);
        Assert.Equal(HvacMode.Cool, store.State.Mode);
    }

    private sealed class FakeThermostatPersistentStateStore : IThermostatPersistentStateStore
    {
        public ThermostatPersistentState State { get; set; } = new();

        public ThermostatPersistentState Load()
        {
            return State;
        }

        public void Save(ThermostatPersistentState state)
        {
            State = state;
        }
    }
}
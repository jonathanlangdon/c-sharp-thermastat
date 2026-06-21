using HvacController.Models;

namespace HvacController.Services;

public interface IThermostatPersistentStateStore
{
    ThermostatPersistentState Load();
    void Save(ThermostatPersistentState state);
}

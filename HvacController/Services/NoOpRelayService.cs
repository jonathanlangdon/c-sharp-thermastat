namespace HvacController.Services;

public sealed class NoOpRelayService : IRelayService
{
    public void AllOff()
    {
    }

    public void SetRelays(bool heat, bool cool, bool fan)
    {
    }
}
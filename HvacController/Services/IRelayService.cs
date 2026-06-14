namespace HvacController.Services;

public interface IRelayService
{
    void AllOff();

    void SetRelays(bool heat, bool cool, bool fan);
}
using PlayHouse.Production.Shared;

namespace PlayHouse.Production.Api;


public class ControllerTester
{
    private  ApiReflection? _apiReflection;

    internal void Init(ApiReflection apiReflection)
    {
        _apiReflection = apiReflection;
    }


    public  async Task CallMethodAsync(IPacket packet, IApiSender apiSender)
    {
        await _apiReflection!.CallMethodAsync(packet, apiSender);

    }

    public  async Task CallBackendMethodAsync(IPacket packet, IApiBackendSender apiBackendSender)
    {
        await _apiReflection!.CallBackendMethodAsync(packet, apiBackendSender);
    }
}

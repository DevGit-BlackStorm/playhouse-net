using PlayHouse.Production.Shared;

namespace PlayHouse.Production.Api;

public static class ControllerTester
{
    private static ApiReflection? _apiReflection;

    internal static void Init(ApiReflection? apiReflection)
    {
        _apiReflection = apiReflection;
    }

    public static async Task CallMethodAsync(IPacket packet, IApiSender apiSender)
    {
        await _apiReflection!.CallMethodAsync(packet, apiSender);

    }

    public static async Task CallBackendMethodAsync(IPacket packet, IApiBackendSender apiBackendSender)
    {
        await _apiReflection!.CallBackendMethodAsync( packet, apiBackendSender);
    }
}

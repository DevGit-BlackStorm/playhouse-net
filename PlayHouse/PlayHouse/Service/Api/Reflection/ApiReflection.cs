using PlayHouse.Production.Api.Aspectify;
using PlayHouse.Production.Shared;
using PlayHouse.Service.Api.Reflection;

internal class ApiReflection(IServiceProvider serviceProvider, ApiControllAspectifyManager controllAspectifyManager)
{
    private readonly ApiHandleReflectionInvoker _apiReflectionInvoker = new(serviceProvider,
        controllAspectifyManager.Get(),
        controllAspectifyManager.GetBackend());


    public async Task CallMethodAsync(IPacket packet, IApiSender apiSender)
    {
        await _apiReflectionInvoker.InvokeMethods(packet.MsgId, packet, apiSender);
    }

    public async Task CallBackendMethodAsync(IPacket packet, IApiBackendSender apiBackendSender)
    {
        await _apiReflectionInvoker.InvokeBackendMethods(packet.MsgId, packet, apiBackendSender);
    }
}
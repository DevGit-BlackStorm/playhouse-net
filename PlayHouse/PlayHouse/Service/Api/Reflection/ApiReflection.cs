using PlayHouse.Production.Api.Aspectify;
using PlayHouse.Production.Shared;

namespace PlayHouse.Service.Api.Reflection;

internal class ApiReflection(IServiceProvider serviceProvider ,ApiControllAspectifyManager aspectifyManager)
{
    private readonly ApiHandleReflectionInvoker _apiReflectionInvoker = new(serviceProvider,
        aspectifyManager.Get(),
        aspectifyManager.GetBackend());


    public async Task CallMethodAsync(IPacket packet, IApiSender apiSender)
    {
        await _apiReflectionInvoker.InvokeMethods(serviceProvider,packet.MsgId, packet, apiSender);
    }

    public async Task CallBackendMethodAsync(IPacket packet, IApiBackendSender apiBackendSender)
    {
        await _apiReflectionInvoker.InvokeBackendMethods(serviceProvider,packet.MsgId, packet, apiBackendSender);
    }

    public void Reset(IServiceProvider provider)
    {
        serviceProvider = provider;
    }
}
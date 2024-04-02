using PlayHouse.Production.Api.Aspectify;
using PlayHouse.Production.Shared;
using PlayHouse.Service.Api.Reflection;


internal class ApiReflection
{
    private readonly ApiHandleReflectionInvoker _apiReflectionInvoker;


    public ApiReflection(IServiceProvider serviceProvider,ApiControllAspectifyManager controllAspectifyManager)
    {
        _apiReflectionInvoker = new ApiHandleReflectionInvoker(serviceProvider, controllAspectifyManager.Get());

    }

    public async Task CallMethodAsync(IPacket packet, IApiSender apiSender)
    {
        await _apiReflectionInvoker.InvokeMethods(packet.MsgId, packet,apiSender);

    }

    public async Task CallBackendMethodAsync(IPacket packet, IApiBackendSender apiBackendSender)
    {
        await _apiReflectionInvoker.InvokeBackendMethods(packet.MsgId,  packet, apiBackendSender );
    }

}

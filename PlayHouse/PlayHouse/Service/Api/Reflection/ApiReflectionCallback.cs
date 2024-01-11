using PlayHouse.Production.Api;
using PlayHouse.Production.Shared;
using PlayHouse.Service.Shared.Reflection;
using PlayHouse.Utils;

namespace PlayHouse.Service.Api.Reflection;

internal class ApiReflectionCallback
{
    private readonly CallbackReflectionInvoker _invoker;
    private readonly LOG<ApiReflectionCallback> _log = new();

    public ApiReflectionCallback(IServiceProvider serviceProvider)
    {
        _invoker = new CallbackReflectionInvoker(serviceProvider,new Type[] { typeof(IDisconnectCallback), typeof(IUpdateServerInfoCallback) });
    }

    public async Task OnDisconnectAsync(IApiSender sender)
    {
        await _invoker.InvokeMethods("OnDisconnectAsync", new object[] { sender });
    }

    public async Task<List<IServerInfo>> UpdateServerInfoAsync(IServerInfo serverInfo)
    {
        return (List<IServerInfo>) (await _invoker.InvokeMethodsWithReturn("UpdateServerInfoAsync", new object[] {serverInfo }))!;
    }
}

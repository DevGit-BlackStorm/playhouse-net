using PlayHouse.Production.Api;
using PlayHouse.Production.Shared;
using PlayHouse.Service.Shared.Reflection;
using PlayHouse.Utils;

namespace PlayHouse.Service.Api.Reflection;

internal class ApiReflectionCallback(IServiceProvider serviceProvider)
{
    private readonly CallbackReflectionInvoker _invoker = new(serviceProvider,
        new[] { typeof(IDisconnectCallback), typeof(IUpdateServerInfoCallback) });

    private readonly LOG<ApiReflectionCallback> _log = new();

    public async Task OnDisconnectAsync(IApiSender sender)
    {
        await _invoker.InvokeMethods("OnDisconnectAsync", [sender]);
    }

    public async Task<List<IServerInfo>> UpdateServerInfoAsync(IServerInfo serverInfo)
    {
        return (List<IServerInfo>)(await _invoker.InvokeMethodsWithReturn("UpdateServerInfoAsync",
            [serverInfo]))!;
    }
}
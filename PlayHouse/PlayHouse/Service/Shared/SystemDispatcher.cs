using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using PlayHouse.Service.Shared.Reflection;
using PlayHouse.Utils;

namespace PlayHouse.Service.Shared;

internal class SystemDispatcher(
    ushort serviceId,
    RequestCache requestCache,
    IClientCommunicator clientCommunicator,
    XSystemPanel xSystemPanel,
    IServiceProvider serviceProvider)
{
    private readonly LOG<SystemDispatcher> _log = new();

    private readonly SystemReflection _systemReflection = new(serviceProvider);

    public void Start()
    {
    }


    public void Stop()
    {
    }

    private async Task Dispatch(RoutePacket routePacket)
    {
        using (routePacket)
        {
            var sender = new XSender(serviceId, clientCommunicator, requestCache);
            sender.SetCurrentPacketHeader(routePacket.RouteHeader);
            await _systemReflection.CallMethodAsync(routePacket.ToContentsPacket(), xSystemPanel, sender);
        }
    }

    public void OnPost(RoutePacket routePacket)
    {
        Task.Run(async () => await Dispatch(routePacket));
    }
}
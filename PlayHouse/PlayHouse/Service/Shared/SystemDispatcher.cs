using PlayHouse.Communicator.Message;
using PlayHouse.Communicator;
using PlayHouse.Utils;
using PlayHouse.Service.Shared.Reflection;

namespace PlayHouse.Service.Shared;

internal class SystemDispatcher
{
    private readonly LOG<SystemDispatcher> _log = new();

    private readonly ushort _serviceId;
    private readonly RequestCache _requestCache;
    private readonly IClientCommunicator _clientCommunicator;
    private readonly XSystemPanel _xSystemPanel;
    private readonly SystemReflection _systemReflection;
    //private readonly PacketWorkerQueue _packetWorkerQueue;
    public SystemDispatcher(
        ushort serviceId,
        RequestCache requestCache,
        IClientCommunicator clientCommunicator,
        XSystemPanel xSystemPanel,
        IServiceProvider serviceProvider
        )
    {
        _serviceId = serviceId;
        _requestCache = requestCache;
        _clientCommunicator = clientCommunicator;
        _xSystemPanel = xSystemPanel;
        _systemReflection = new SystemReflection(serviceProvider);
        //_packetWorkerQueue = new PacketWorkerQueue(Dispatch);
    }

    public void Start()
    {
      //  _packetWorkerQueue.Start();
    }

    
    public void Stop()
    {
        //_packetWorkerQueue.Stop();
    }

   
    async Task Dispatch(RoutePacket routePacket)
    {
        using(routePacket)
        {
            XSender sender = new XSender(_serviceId, _clientCommunicator, _requestCache);
            sender.SetCurrentPacketHeader(routePacket.RouteHeader);
            await _systemReflection.CallMethodAsync(routePacket.ToContentsPacket(), _xSystemPanel, sender);
        }
    }

    public void OnPost(RoutePacket routePacket)
    {
        //_packetWorkerQueue.Post(routePacket);
        Task.Run(async () => await Dispatch(routePacket));
    }
}

using PlayHouse.Communicator.Message;
using Playhouse.Protocol;
using PlayHouse.Communicator;
using PlayHouse.Utils;
using PlayHouse.Service.Shared.Reflection;

namespace PlayHouse.Service.Shared
{
    internal class SystemDispatcher
    {
        private readonly LOG<SystemDispatcher> _log = new();

        private readonly ushort _serviceId;
        private readonly RequestCache _requestCache;
        private readonly IClientCommunicator _clientCommunicator;
        private readonly XSystemPanel _xSystemPanel;

        private readonly SystemReflection _systemReflection;

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
        }


        public async Task DispatchAsync(RoutePacket routePacket)
        {
            using(routePacket)
            {
                //if (routePacket.IsReply())
                //{
                //    _requestCache.OnReply(routePacket);
                //    return;
                //}

                XSender sender = new XSender(_serviceId, _clientCommunicator, _requestCache);
                sender.SetCurrentPacketHeader(routePacket.RouteHeader);
                await _systemReflection.CallMethodAsync(routePacket.ToContentsPacket(), _xSystemPanel, sender);
              
            }
        }
    }
}

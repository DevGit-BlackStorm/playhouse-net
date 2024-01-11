using PlayHouse.Communicator.Message;
using PlayHouse.Communicator;
using PlayHouse.Utils;
using PlayHouse.Production.Session;
using PlayHouse.Production.Shared;

namespace PlayHouse.Service.Session
{
    internal class SessionService : IService
    {
        private readonly LOG<SessionService> _log = new ();
        private readonly ushort _serviceId;
        private readonly SessionDispatcher _sessionDispatcher;

        private readonly AtomicEnum<ServerState> _state = new(ServerState.DISABLE);
        private readonly PerformanceTester _performanceTester;


        public ushort ServiceId => _serviceId;

        public SessionService(
            ushort serviceId,
            SessionOption sessionOption,
            IServerInfoCenter serverInfoCenter,
            IClientCommunicator clientCommunicator,
            RequestCache requestCache,
            bool showQps
        )
        {

            _serviceId = serviceId;
            _performanceTester = new PerformanceTester(showQps, "client");
            _sessionDispatcher = new SessionDispatcher(serviceId,sessionOption,serverInfoCenter,clientCommunicator,requestCache);
        }

        public void OnStart()
        {
            _state.Set(ServerState.RUNNING);

            _sessionDispatcher.Start();
            _performanceTester.Start();

        }

        public async Task OnDispatchAsync(RoutePacket routePacket)
        {
            await _sessionDispatcher.DispatchAsync(routePacket);
        }


        public void OnStop()
        {

            _performanceTester.Stop();
            _sessionDispatcher.Stop();

            _state.Set(ServerState.DISABLE);
        }

        public ServerState GetServerState()
        {
            return _state.Get();
        }

        public ServiceType GetServiceType()
        {
            return ServiceType.SESSION;
        }

        public ushort GetServiceId()
        {
            return _serviceId;
        }

        public void Pause()
        {
            _state.Set(ServerState.PAUSE);
        }

        public void Resume()
        {
            _state.Set(ServerState.RUNNING);
        }

        public int GetActorCount()
        {
            return _sessionDispatcher.GetActorCount();
        }
    }
}

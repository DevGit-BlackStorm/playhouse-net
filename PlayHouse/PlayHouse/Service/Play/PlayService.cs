using PlayHouse.Communicator.Message;
using PlayHouse.Communicator;
using PlayHouse.Utils;
using PlayHouse.Production.Play;
using PlayHouse.Production.Shared;

namespace PlayHouse.Service.Play
{
    internal class PlayService : IService
    {
        private readonly LOG<PlayService> _log = new ();
        private readonly AtomicEnum<ServerState> _state = new(ServerState.DISABLE);
        private readonly PlayDispatcher _playDispatcher;

        public ushort ServiceId { get; }

        public PlayService(ushort serviceId, string publicEndpoint, PlayOption playOption,
            IClientCommunicator clientCommunicator, RequestCache requestCache, IServerInfoCenter serverInfoCenter)
        {
            ServiceId = serviceId;
            _playDispatcher = new PlayDispatcher(serviceId, clientCommunicator, requestCache, serverInfoCenter, publicEndpoint, playOption);
        }

        public  void OnStart()
        {
            _state.Set(ServerState.RUNNING);
        }

        
        public void OnStop()
        {
            _state.Set(ServerState.DISABLE);
        }
        
        public ServerState GetServerState()
        {
            return _state.Get();
        }

        public ServiceType GetServiceType()
        {
            return ServiceType.Play;
        }

        public void OnPause()
        {
            _state.Set(ServerState.PAUSE);
        }

        public void ONResume()
        {
            _state.Set(ServerState.RUNNING);
        }

        public async Task OnDispatchAsync(RoutePacket routePacket)
        {
            await _playDispatcher.DispatchAsync(routePacket);
        }

        public int GetActorCount()
        {
           return _playDispatcher.GetActorCount();
        }
    }

}

using PlayHouse.Communicator.Message;
using PlayHouse.Communicator;
using PlayHouse.Utils;
using PlayHouse.Production.Api;
using PlayHouse.Production.Shared;

namespace PlayHouse.Service.Api
{
    internal class ApiService : IService
    {
        private readonly LOG<ApiService> _log = new ();
        private readonly ushort _serviceId;

        private readonly ApiDispatcher _apiDispatcher;
        private readonly AtomicEnum<ServerState> _state = new(ServerState.DISABLE);        
        
        
        public ushort ServiceId => _serviceId;


        public ApiService(
            ushort serviceId,
            ApiOption apiOption,
            RequestCache requestCache,
            IClientCommunicator clientCommunicator,
            IServiceProvider serviceProvider
        )
        {
            _serviceId = serviceId;
            _apiDispatcher = new ApiDispatcher(serviceId, requestCache, clientCommunicator, serviceProvider,apiOption);

        }

        public void OnStart()
        {
            _state.Set(ServerState.RUNNING);
            _apiDispatcher.Start();
        }

        public void OnStop()
        {
            _state.Set(ServerState.DISABLE);
            _apiDispatcher.Stop();
        }


        public ServiceType GetServiceType()
        {
            return ServiceType.API;
        }

        public void OnPause()
        {
            _state.Set(ServerState.PAUSE);
        }
        public void OnResume()
        {
            _state.Set(ServerState.RUNNING);
        }

        public ushort GetServiceId()
        {
            return _serviceId;
        }

        public ServerState GetServerState()
        {
            return _state.Get();
        }

        public int GetActorCount()
        {
            return _apiDispatcher.GetAccountCount();
        }

        public void OnPost(RoutePacket routePacket)
        {
            _apiDispatcher.OnPost(routePacket);
        }
    }

}

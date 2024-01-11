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
            _apiDispatcher = new ApiDispatcher(serviceId, requestCache, clientCommunicator, serviceProvider);

        }

        public void OnStart()
        {
            _state.Set(ServerState.RUNNING);
        }

     

        public async Task OnDispatchAsync(RoutePacket routePacket)
        {
            await _apiDispatcher.DispatchAsync(routePacket);  
        }

        public void OnStop()
        {
        }


        public ServiceType GetServiceType()
        {
            return ServiceType.API;
        }

        public void Pause()
        {
            _state.Set(ServerState.PAUSE);
        }
        public void Resume()
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
    }

}

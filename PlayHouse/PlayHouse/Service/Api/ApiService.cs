using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using PlayHouse.Production.Api;
using PlayHouse.Production.Shared;
using PlayHouse.Utils;

namespace PlayHouse.Service.Api;

internal class ApiService(
    ushort serviceId,
    ApiOption apiOption,
    RequestCache requestCache,
    IClientCommunicator clientCommunicator,
    IServiceProvider serviceProvider)
    : IService
{
    private readonly ApiDispatcher _apiDispatcher = new(serviceId, requestCache, clientCommunicator, serviceProvider, apiOption);
    private readonly LOG<ApiService> _log = new();
    private readonly AtomicEnum<ServerState> _state = new(ServerState.DISABLE);


    public ushort ServiceId { get; } = serviceId;

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

    public ushort GetServiceId()
    {
        return ServiceId;
    }
}
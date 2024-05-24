using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using PlayHouse.Production.Session;
using PlayHouse.Production.Shared;
using PlayHouse.Utils;

namespace PlayHouse.Service.Session;

internal class SessionService : IService
{
    private readonly LOG<SessionService> _log = new();
    private readonly PerformanceTester _performanceTester;
    private readonly SessionDispatcher _sessionDispatcher;

    private readonly AtomicEnum<ServerState> _state = new(ServerState.DISABLE);

    public SessionService(
        ushort serviceId,
        SessionOption sessionOption,
        IServerInfoCenter serverInfoCenter,
        IClientCommunicator clientCommunicator,
        RequestCache requestCache,
        bool showQps
    )
    {
        ServiceId = serviceId;
        _performanceTester = new PerformanceTester(showQps, "client");
        _sessionDispatcher =
            new SessionDispatcher(serviceId, sessionOption, serverInfoCenter, clientCommunicator, requestCache);
    }


    public ushort ServiceId { get; }

    public void OnStart()
    {
        _state.Set(ServerState.RUNNING);

        _sessionDispatcher.Start();
        _performanceTester.Start();
    }

    public void OnPost(RoutePacket routePacket)
    {
        _sessionDispatcher.OnPost(routePacket);
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

    public void OnPause()
    {
        _state.Set(ServerState.PAUSE);
    }

    public void OnResume()
    {
        _state.Set(ServerState.RUNNING);
    }

    public int GetActorCount()
    {
        return _sessionDispatcher.GetActorCount();
    }

    public ushort GetServiceId()
    {
        return ServiceId;
    }
}
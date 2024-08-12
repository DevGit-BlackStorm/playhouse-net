using Google.Protobuf;
using PlayHouse.Production.Shared;
using Playhouse.Protocol;

namespace PlayHouse.Communicator;

internal class XServerInfo : IServerInfo
{
    private readonly string _bindEndpoint = string.Empty;
    private readonly ushort _serviceId;
    private readonly ServiceType _serviceType;
    private int _actorCount;
    private long _lastUpdate;
    private ServerState _serverState;

    public XServerInfo(
        string bindEndpoint,
        ServiceType serviceType,
        ushort serviceId,
        ServerState state,
        int actorCount,
        long lastUpdate)
    {
        _bindEndpoint = bindEndpoint;
        _serviceType = serviceType;
        _serviceId = serviceId;
        _serverState = state;
        _actorCount = actorCount;
        _lastUpdate = lastUpdate;
    }

    public string GetBindEndpoint()
    {
        return _bindEndpoint;
    }

    public ServiceType GetServiceType()
    {
        return _serviceType;
    }

    public ushort GetServiceId()
    {
        return _serviceId;
    }

    public ServerState GetState()
    {
        return _serverState;
    }

    public long GetLastUpdate()
    {
        return _lastUpdate;
    }

    public int GetActorCount()
    {
        return _actorCount;
    }

    public static XServerInfo Of(string bindEndpoint, IService service)
    {
        return new XServerInfo(
            bindEndpoint,
            service.GetServiceType(),
            service.ServiceId,
            service.GetServerState(),
            service.GetActorCount(),
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        );
    }

    public static XServerInfo Of(ServerInfoMsg infoMsg)
    {
        return new XServerInfo(
            infoMsg.Endpoint,
            Enum.Parse<ServiceType>(infoMsg.ServiceType),
            (ushort)infoMsg.ServiceId,
            Enum.Parse<ServerState>(infoMsg.ServerState),
            infoMsg.ActorCount,
            infoMsg.Timestamp
        );
    }

    public static XServerInfo Of(
        string bindEndpoint,
        ServiceType serviceType,
        ushort serviceId,
        ServerState state,
        int actorCount,
        long timeStamp)
    {
        return new XServerInfo(
            bindEndpoint,
            serviceType,
            serviceId,
            state,
            actorCount,
            timeStamp);
    }

    public static XServerInfo Of(IServerInfo serverInfo)
    {
        return new XServerInfo(
            serverInfo.GetBindEndpoint(),
            serverInfo.GetServiceType(),
            serverInfo.GetServiceId(),
            serverInfo.GetState(),
            serverInfo.GetActorCount(),
            serverInfo.GetLastUpdate());
    }

    public ServerInfoMsg ToMsg()
    {
        return new ServerInfoMsg
        {
            ServiceType = _serviceType.ToString(),
            ServiceId = _serviceId,
            Endpoint = _bindEndpoint,
            ServerState = _serverState.ToString(),
            Timestamp = _lastUpdate,
            ActorCount = _actorCount
        };
    }

    public bool TimeOver()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _lastUpdate > 60000;
    }

    public bool Update(XServerInfo serverInfo)
    {
        var stateChanged = GetState != serverInfo.GetState;

        _serverState = serverInfo.GetState();

        _lastUpdate = serverInfo.GetLastUpdate();

        _actorCount = serverInfo.GetActorCount();

        return stateChanged;
    }

    public bool IsValid()
    {
        return _serverState == ServerState.RUNNING ;
    }

    public byte[] ToByteArray()
    {
        return ToMsg().ToByteArray();
    }

    public bool CheckTimeout()
    {
        if (TimeOver())
        {
            _serverState = ServerState.DISABLE;
            return true;
        }

        return false;
    }


    public override string ToString()
    {
        return
            $"[endpoint: {GetBindEndpoint}, service type: {GetServiceType}, serviceId: {GetServiceId}, state: {GetState}, actor count: {GetActorCount}, GetLastUpdate: {GetLastUpdate}]";
    }

    internal void SetState(ServerState state)
    {
        _serverState = state;
    }

    internal void SetLastUpdate(long updatTime)
    {
        _lastUpdate = updatTime;
    }
}
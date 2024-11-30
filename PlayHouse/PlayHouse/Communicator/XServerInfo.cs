using Google.Protobuf;
using PlayHouse.Production.Shared;
using Playhouse.Protocol;

namespace PlayHouse.Communicator;

internal class XServerInfo(
    string bindEndpoint,
    int nid,
    ServiceType serviceType,
    ushort serviceId,
    ServerState serverState,
    int actorCount,
    long lastUpdate)
    : IServerInfo
{
    private int _nid = nid;

    public string GetBindEndpoint()
    {
        return bindEndpoint;
    }

    public int GetNid()
    {
        return _nid;
    }

    public ServiceType GetServiceType()
    {
        return serviceType;
    }

    public ushort GetServiceId()
    {
        return serviceId;
    }

    public ServerState GetState()
    {
        return serverState;
    }

    public long GetLastUpdate()
    {
        return lastUpdate;
    }

    public int GetActorCount()
    {
        return actorCount;
    }

    public static XServerInfo Of(string bindEndpoint, IService service)
    {
        return new XServerInfo(
            bindEndpoint,
            service.Nid,
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
            infoMsg.Nid,
            Enum.Parse<ServiceType>(infoMsg.ServiceType),
            (ushort)infoMsg.ServiceId,
            Enum.Parse<ServerState>(infoMsg.ServerState),
            infoMsg.ActorCount,
            infoMsg.Timestamp
        );
    }

    public static XServerInfo Of(
        string bindEndpoint,
        int nid,
        ServiceType serviceType,
        ushort serviceId,
        ServerState state,
        int actorCount,
        long timeStamp)
    {
        return new XServerInfo(
            bindEndpoint,
            nid,
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
            serverInfo.GetNid(),
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
            ServiceType = serviceType.ToString(),
            ServiceId = serviceId,
            Endpoint = bindEndpoint,
            ServerState = serverState.ToString(),
            Timestamp = lastUpdate,
            ActorCount = actorCount
        };
    }

    public bool TimeOver()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - lastUpdate > 60000;
    }

    public void Update(XServerInfo serverInfo)
    {
        //var stateChanged = GetState != serverInfo.GetState;

        serverState = serverInfo.GetState();

        lastUpdate = serverInfo.GetLastUpdate();

        actorCount = serverInfo.GetActorCount();

        _nid = serverInfo._nid;
    }

    public bool IsValid()
    {
        return serverState == ServerState.RUNNING ;
    }

    public byte[] ToByteArray()
    {
        return ToMsg().ToByteArray();
    }

    public bool CheckTimeout()
    {
        if (TimeOver())
        {
            serverState = ServerState.DISABLE;
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
        serverState = state;
    }

    internal void SetLastUpdate(long updatTime)
    {
        lastUpdate = updatTime;
    }
}
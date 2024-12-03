using PlayHouse.Communicator;

namespace PlayHouse.Production.Shared;

public enum ServerState
{
    RUNNING,
    PAUSE,
    DISABLE
}

public interface IServerInfo
{
    string GetBindEndpoint();

    string GetNid();
    int GetServerId();
    ServiceType GetServiceType();
    ushort GetServiceId();
    ServerState GetState();
    long GetLastUpdate();
    int GetActorCount();
}
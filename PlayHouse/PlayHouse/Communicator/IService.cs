using PlayHouse.Communicator.Message;
using PlayHouse.Production.Shared;

namespace PlayHouse.Communicator;
public enum ServiceType
{
    SESSION,
    API,
    Play
}

internal interface IService
{
    ushort ServiceId { get; }
    void OnStart();
    Task OnDispatchAsync(RoutePacket routePacket);
    void OnStop();
    int GetActorCount();
    ServerState GetServerState();
    ServiceType GetServiceType();
    void OnPause();
    void ONResume();
}
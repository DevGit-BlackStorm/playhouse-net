using PlayHouse.Communicator.Message;
using PlayHouse.Production;

namespace PlayHouse.Communicator;
public enum ServiceType
{
    SESSION,
    API,
    Play
}

internal interface IProcessor
{
    ushort ServiceId { get; }
    void OnStart();
    void OnReceive(RoutePacket routePacket);
    void OnStop();
    int GetWeightPoint();
    ServerState GetServerState();
    ServiceType GetServiceType();
    void Pause();
    void Resume();
}
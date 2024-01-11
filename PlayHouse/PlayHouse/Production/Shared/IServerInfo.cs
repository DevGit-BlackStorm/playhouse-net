using PlayHouse.Communicator;

namespace PlayHouse.Production.Shared
{

    public enum ServerState
    {
        RUNNING,
        PAUSE,
        DISABLE
    }

    public interface IServerInfo
    {
        string BindEndpoint { get; }
        ServiceType ServiceType { get; }
        ushort ServiceId { get; }
        ServerState State { get; }
        long LastUpdate { get; }
        int ActorCount { get; }
    }
}

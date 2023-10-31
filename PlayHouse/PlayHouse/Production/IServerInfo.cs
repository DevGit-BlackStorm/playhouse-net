using PlayHouse.Communicator;

namespace PlayHouse.Production
{

    public enum ServerState
    {
        RUNNING,
        PAUSE,
        DISABLE
    }


    public interface IServerInfo
    {
        string BindEndpoint();
        ServiceType ServiceType();
        ushort ServiceId();
        ServerState State();
        //int WeightingPoint();
        long TimeStamp();
    }
}

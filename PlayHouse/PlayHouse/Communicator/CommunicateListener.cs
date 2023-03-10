using PlayHouse.Communicator.Message;

namespace PlayHouse.Communicator
{
    public interface ICommunicateListener
    {
        void OnReceive(RoutePacket routePacket);
     }
}
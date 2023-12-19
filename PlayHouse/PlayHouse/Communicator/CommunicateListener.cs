using PlayHouse.Communicator.Message;

namespace PlayHouse.Communicator
{
    interface ICommunicateListener
    {
        public void OnReceive(RoutePacket routePacket);
     }
}
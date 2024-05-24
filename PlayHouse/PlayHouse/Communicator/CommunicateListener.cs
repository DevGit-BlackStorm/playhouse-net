using PlayHouse.Communicator.Message;

namespace PlayHouse.Communicator;

internal interface ICommunicateListener
{
    public void OnReceive(RoutePacket routePacket);
}
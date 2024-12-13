using PlayHouse.Communicator.Message;

namespace PlayHouse.Communicator;

internal interface IClientCommunicator
{
    void Connect(string nid, string endpoint);
    void Send(string nid, RoutePacket routePacket);
    void Communicate();
    void Disconnect(string nid, string endpoint);
    void Stop();
}
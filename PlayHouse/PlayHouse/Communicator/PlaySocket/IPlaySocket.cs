using PlayHouse.Communicator.Message;

namespace PlayHouse.Communicator.PlaySocket;

internal interface IPlaySocket
{
    string GetBindEndpoint();
    void Bind();
    void Send(string endpoint, RoutePacket routerPacket);
    void Connect(string target);
    RoutePacket? Receive();
    void Disconnect(string endpoint);

    void Close();

    string Id();
}
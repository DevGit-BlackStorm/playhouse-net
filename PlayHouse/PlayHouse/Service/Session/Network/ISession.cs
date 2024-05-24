using PlayHouse.Communicator.Message;

namespace PlayHouse.Service.Session.Network;

internal interface ISession
{
    void ClientDisconnect();
    void Send(ClientPacket packet);
}
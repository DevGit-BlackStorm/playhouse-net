using PlayHouse.Communicator.Message;

namespace PlayHouse.Service.Session.Network
{
    public interface ISession
    {
        void ClientDisconnect();
        void Send(ClientPacket packet);
    }
}

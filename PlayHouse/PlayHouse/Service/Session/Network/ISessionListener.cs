using PlayHouse.Communicator.Message;

namespace PlayHouse.Service.Session.Network;

internal interface ISessionListener
{
    void OnConnect(int sid, ISession session);
    void OnReceive(int sid, ClientPacket clientPacket);
    void OnDisconnect(int sid);
}
using PlayHouse.Communicator.Message;

namespace PlayHouse.Service.Session.Network;

internal interface ISessionListener
{
    void OnConnect(long sid, ISession session,string remoteIp);
    void OnReceive(long sid, ClientPacket clientPacket);
    void OnDisconnect(long sid);
}
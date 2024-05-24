using PlayHouse.Production.Session;
using PlayHouse.Service.Session.Network.tcp;
using PlayHouse.Service.Session.Network.websocket;

namespace PlayHouse.Service.Session.Network;

internal class SessionNetwork
{
    private readonly ISessionNetwork _sessionNetwork;

    public SessionNetwork(SessionOption sessionOption, ISessionListener sessionListener)
    {
        if (sessionOption.UseWebSocket)
        {
            _sessionNetwork = new WsSessionNetwork(sessionOption, sessionListener);
        }
        else
        {
            _sessionNetwork = new TcpSessionNetwork(sessionOption, sessionListener);
        }
    }

    public void Start()
    {
        _sessionNetwork.Start();
    }

    public void Stop()
    {
        _sessionNetwork.Stop();
    }

    public void Await()
    {
        //_sessionThread!.Join();
    }
}
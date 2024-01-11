using NetCoreServer;
using PlayHouse.Communicator.Message;
using System.Net.Sockets;
using CommonLib;
using PlayHouse.Production.Session;
using PlayHouse.Utils;

namespace PlayHouse.Service.Session.Network.websocket;
class XWsSession : WsSession, ISession
{
    private readonly LOG<XWsSession> _log = new ();
    private readonly PacketParser _packetParser;
    private readonly ISessionListener _sessionListener;
    private readonly RingBuffer _buffer = new RingBuffer(1024 * 8, 1024 * 64 * 4);
    private readonly RingBufferStream _stream;

    public XWsSession(WsSessionServer server, ISessionListener sessionListener) : base(server)
    {
        _packetParser = new PacketParser();
        _sessionListener = sessionListener;
        _stream = new RingBufferStream(_buffer);
    }

    private int GetSid()
    {
        return (int)Socket.Handle;
    }
    protected override void OnConnected()
    {
        try
        {
            _log.Debug(()=>$"WS session OnConnected - [Sid:{GetSid()}]");
            _sessionListener.OnConnect(GetSid(), this);
        }
        catch (Exception e)
        {
            _log.Error(()=>e.ToString());
        }

      

    }

    protected override void OnDisconnected()
    {
        try
        {
            _log.Debug(()=>$"WS session OnDisConnected - [Sid:{GetSid()}]");
            _sessionListener.OnDisconnect(GetSid());
        }
        catch(Exception e)
        {
            _log.Error(()=>e.ToString());
        }
        
    }

    protected override void OnReceived(byte[] buffer, long offset, long size)
    {
        try
        {
            _stream.Write(buffer, (int)offset, (int)size);
            List<ClientPacket> packets = _packetParser.Parse(_buffer);
            foreach (ClientPacket packet in packets)
            {
                _sessionListener.OnReceive(GetSid(), packet);
            }
        }
        catch (Exception e)
        {
            _log.Error(()=>e.ToString());
        }
    }

    protected override void OnError(SocketError error)
    {
        try
        {
            _log.Error(()=>$"socket caught an error - [codeCode:{error}]");
            Disconnect();
        }
        catch(Exception e)
        {
            _log.Error(()=>e.ToString());
        }
    }

    public void ClientDisconnect()
    {
        base.Disconnect();
    }

    public void Send(ClientPacket packet)
    {
        using(packet)
        {
            base.Send(packet.Data);
        }
    }
}
internal class WsSessionServer : WsServer
{
    private ISessionListener _sessionListener;
    public WsSessionServer(string address, int port, ISessionListener sessionListener) : base(address, port)
    {
        _sessionListener = sessionListener;

        OptionNoDelay = true;
        OptionReuseAddress = true;
        OptionKeepAlive = true;

        OptionReceiveBufferSize = 64 * 1024;
        OptionSendBufferSize = 64 * 1024;
        OptionAcceptorBacklog = 1024;
    }

    protected override WsSession CreateSession()
    {
        return new XWsSession(this, _sessionListener);
    }
}
class WsSessionNetwork : ISessionNetwork
{
    private readonly LOG<WsSessionNetwork> _log = new ();
    private readonly WsSessionServer _wsSessionServer;
    
    public WsSessionNetwork(SessionOption sessionOption, ISessionListener sessionListener)
    {
        //_wsSessionServer = new WsSessionServer(IpFinder.FindLocalIp(), sessionOption.SessionPort, sessionListener);
        _wsSessionServer = new WsSessionServer("0.0.0.0", sessionOption.SessionPort, sessionListener);
    }

    public void Start()
    {
        if (_wsSessionServer.Start())
        {
            _log.Info(()=>"WsSessionNetwork Start");
        }
        else
        {
            _log.Fatal(()=>"WsSessionNetwork Start Fail");
            Environment.Exit(0);
        }
    }

    public void Stop()
    {
        _log.Info(()=>"WsSessionNetwork StopAsync");
        _wsSessionServer.Stop();
    }
}

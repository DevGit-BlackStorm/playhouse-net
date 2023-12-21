using CommonLib;
using NetCoreServer;
using PlayHouse.Communicator.Message;
using PlayHouse.Production.Session;
using System.Net.Sockets;
using PlayHouse.Utils;

namespace PlayHouse.Service.Session.Network.tcp;
internal class XTcpSession : TcpSession, ISession
{
    private readonly LOG<XTcpSession> _log = new ();
    private readonly PacketParser _packetParser;
    private readonly ISessionListener _sessionListener;
    private readonly RingBuffer _buffer = new RingBuffer(1024 * 8 ,1024*64*4);
    private readonly RingBufferStream _stream;
        

    public XTcpSession(TcpServer server,ISessionListener sessionListener) : base(server)
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
            _log.Debug(()=>$"TCP session OnConnected - [Sid:{GetSid()}]");
            _sessionListener.OnConnect(GetSid(), this);
        }catch (Exception e)
        {
            _log.Error(()=>e.ToString());
        }
    }

    protected override void OnDisconnected()
    {
        try
        {
            _log.Debug(()=>$"TCP session OnDisConnected - [Sid:{GetSid()}]");
            _sessionListener.OnDisconnect(GetSid());
        }
        catch (Exception e)
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
                _log.Trace(() => $"OnReceive from:client - [packetInfo:{packet.Header}]");
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
        using (packet)
        {
            base.SendAsync(packet.Data);
        }
    }
}
internal class TcpSessionServer : TcpServer
{
    private readonly LOG<TcpSessionServer> _log = new ();
    
    private readonly ISessionListener _sessionListener;
    public TcpSessionServer(string address, int port,ISessionListener sessionListener) : base(address, port)
    {
        _sessionListener = sessionListener;

        OptionNoDelay = true;
        OptionReuseAddress = true;
        OptionKeepAlive = true;

        OptionReceiveBufferSize = 64 * 1024;
        OptionSendBufferSize = 64 * 1024;
        OptionAcceptorBacklog = 1024;
    }

    protected override TcpSession CreateSession()
    {
        return new XTcpSession(this,_sessionListener);
    }

    protected override void OnStarted()
    {
        _log.Info(()=>"Server Started");
        
    }
}
class TcpSessionNetwork : ISessionNetwork
{
    private readonly LOG<TcpSessionNetwork> _log = new ();
    private readonly TcpSessionServer _tcpSessionServer;
    
    public TcpSessionNetwork(SessionOption sessionOption,ISessionListener sessionListener) {

        //_tcpSessionServer = new TcpSessionServer(IpFinder.FindLocalIp(), sessionOption.SessionPort, sessionListener);
        _tcpSessionServer = new TcpSessionServer("0.0.0.0", sessionOption.SessionPort, sessionListener);
    }
    
    public void Start()
    {
        if (_tcpSessionServer.Start())
        {
            _log.Info(()=>"TcpSessionNetwork Start");
        }
        else
        {
            _log.Fatal(()=>"Session Server Start Fail");
            Environment.Exit(0);
        }
    }

    public void Stop()
    {
        _log.Info(()=>"TcpSessionNetwork Stop");
        _tcpSessionServer.Stop();
    }
}

using System.Net.Sockets;
using CommonLib;
using NetCoreServer;
using PlayHouse.Communicator.Message;
using PlayHouse.Production.Session;
using PlayHouse.Utils;

namespace PlayHouse.Service.Session.Network.tcp;

internal class XTcpSession(TcpServer server, ISessionListener sessionListener) : TcpSession(server), ISession
{
    private readonly RingBuffer _buffer = new(1024 * 4, PacketConst.MaxPacketSize);
    private readonly LOG<XTcpSession> _log = new();
    private readonly PacketParser _packetParser = new();


    public void ClientDisconnect()
    {
        //base.Disconnect();
    }

    public void Send(ClientPacket packet)
    {
        using (packet)
        {
            base.SendAsync(packet.Span);
        }
    }

    private long GetSid()
    {
        return Socket.Handle.ToInt64();
    }

    protected override void OnConnected()
    {
        try
        {
            _log.Debug(() => $"TCP session OnConnected - [Sid:{GetSid()}]");
            sessionListener.OnConnect(GetSid(), this);
        }
        catch (Exception e)
        {
            _log.Error(() => e.ToString());
        }
    }

    protected override void OnDisconnected()
    {
        try
        {
            _log.Debug(() => $"TCP session OnDisConnected - [Sid:{GetSid()}]");
            sessionListener.OnDisconnect(GetSid());
        }
        catch (Exception e)
        {
            _log.Error(() => e.ToString());
        }
    }

    protected override void OnReceived(byte[] buffer, long offset, long size)
    {
        try
        {
            _buffer.Write(buffer, offset, size);
            var packets = _packetParser.Parse(_buffer);
            foreach (var packet in packets)
            {
                _log.Trace(() => $"OnReceive from:client - [packetInfo:{packet.Header}]");
                sessionListener.OnReceive(GetSid(), packet);
            }
        }
        catch (Exception e)
        {
            _log.Error(() => e.ToString());
            Disconnect();
        }
    }

    protected override void OnError(SocketError error)
    {
        try
        {
            _log.Error(() => $"socket caught an error - [codeCode:{error}]");
            Disconnect();
        }
        catch (Exception e)
        {
            _log.Error(() => e.ToString());
        }
    }
}

internal class TcpSessionServer : TcpServer
{
    private readonly LOG<TcpSessionServer> _log = new();

    private readonly ISessionListener _sessionListener;

    public TcpSessionServer(string address, int port, ISessionListener sessionListener) : base(address, port)
    {
        _sessionListener = sessionListener;

        OptionNoDelay = true;
        OptionReuseAddress = true;
        OptionKeepAlive = true;

        OptionReceiveBufferSize = 64 * 1024;
        OptionSendBufferSize = 64 * 1024;
        OptionAcceptorBacklog = 4096;
    }

    protected override TcpSession CreateSession()
    {
        return new XTcpSession(this, _sessionListener);
    }

    protected override void OnStarted()
    {
        _log.Info(() => "Server Started");
    }
}

internal class TcpSessionNetwork(SessionOption sessionOption, ISessionListener sessionListener)
    : ISessionNetwork
{
    private readonly LOG<TcpSessionNetwork> _log = new();
    private readonly TcpSessionServer _tcpSessionServer = new("0.0.0.0", sessionOption.SessionPort, sessionListener);

    public void Start()
    {
        if (_tcpSessionServer.Start())
        {
            _log.Info(() => "TcpSessionNetwork Start");
        }
        else
        {
            _log.Fatal(() => "Session Server Start Fail");
            Environment.Exit(0);
        }
    }

    public void Stop()
    {
        _log.Info(() => "TcpSessionNetwork StopAsync");
        _tcpSessionServer.Stop();
    }
}
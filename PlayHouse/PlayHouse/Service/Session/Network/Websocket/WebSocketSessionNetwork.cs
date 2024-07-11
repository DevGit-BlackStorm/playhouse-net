using System.Net.Sockets;
using CommonLib;
using NetCoreServer;
using PlayHouse.Communicator.Message;
using PlayHouse.Production.Session;
using PlayHouse.Utils;

namespace PlayHouse.Service.Session.Network.websocket;

internal class XWsSession(WsSessionServer server, ISessionListener sessionListener) : WsSession(server), ISession
{
    private readonly RingBuffer _buffer = new(1024 * 4, PacketConst.MaxPacketSize);
    private readonly LOG<XWsSession> _log = new();
    private readonly PacketParser _packetParser = new();

    public void ClientDisconnect()
    {
        base.Disconnect();
    }

    public void Send(ClientPacket packet)
    {
        using (packet)
        {
            base.Send(packet.Span);
        }
    }

    private int GetSid()
    {
        return (int)Socket.Handle;
    }

    protected override void OnConnected()
    {
        try
        {
            _log.Debug(() => $"WS session OnConnected - [Sid:{GetSid()}]");
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
            _log.Debug(() => $"WS session OnDisConnected - [Sid:{GetSid()}]");
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
            List<ClientPacket> packets;

            lock (_buffer)
            {
                _buffer.Write(buffer, offset, size);
                packets = _packetParser.Parse(_buffer);
            }

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

internal class WsSessionServer : WsServer
{
    private readonly ISessionListener _sessionListener;

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

internal class WsSessionNetwork(SessionOption sessionOption, ISessionListener sessionListener)
    : ISessionNetwork
{
    private readonly LOG<WsSessionNetwork> _log = new();
    private readonly WsSessionServer _wsSessionServer = new("0.0.0.0", sessionOption.SessionPort, sessionListener);

    public void Start()
    {
        if (_wsSessionServer.Start())
        {
            _log.Info(() => "WsSessionNetwork Start");
        }
        else
        {
            _log.Fatal(() => "WsSessionNetwork Start Fail");
            Environment.Exit(0);
        }
    }

    public void Stop()
    {
        _log.Info(() => "WsSessionNetwork StopAsync");
        _wsSessionServer.Stop();
    }
}
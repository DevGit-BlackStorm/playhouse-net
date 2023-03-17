using NetCoreServer;
using PlayHouse.Communicator.Message.buffer;
using PlayHouse.Communicator.Message;
using PlayHouse.Communicator;
using System.Net.Sockets;

namespace PlayHouse.Service.Session.network.websocket
{
    class XWsSession : WsSession
    {
        private ILogger _log;
        private PacketParser _packetParser;
        private ISessionListener _sessionListener;

        public XWsSession(WsSessionServer server, ISessionListener sessionListener, ILogger log) : base(server)
        {
            _log = log;
            _packetParser = new PacketParser(log);
            _sessionListener = sessionListener;
        }

        private int GetSid()
        {
            return (int)Socket.Handle;
        }
        protected override void OnConnected()
        {

            _log.Info($"Websocket session with Id {GetSid()} connected!", typeof(XWsSession).Name);
            _sessionListener.OnConnect(GetSid());

        }

        protected override void OnDisconnected()
        {
            _log.Info($"Websocket session with Id {GetSid()} disconnected!", typeof(XWsSession).Name);
            _sessionListener.OnDisconnect(GetSid());
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            var pooledBuffer = new PooledBuffer(size);
            pooledBuffer.Append(buffer, offset, size);

            List<ClientPacket> packets = _packetParser.Parse(pooledBuffer);
            foreach (ClientPacket packet in packets)
            {
                _sessionListener.OnReceive(GetSid(), packet);
            }
        }

        protected override void OnError(SocketError error)
        {
            _log.Error($"Chat TCP session caught an error with code {error}", typeof(XWsSession).Name);
            Disconnect();
        }
    }
    public class WsSessionServer : WsServer
    {
        private ISessionListener _sessionListener;
        private ILogger _log;
        public WsSessionServer(string address, int port, ISessionListener sessionListener, ILogger log) : base(address, port)
        {
            _sessionListener = sessionListener;
            _log = log;
        }

        protected override WsSession CreateSession()
        {
            return new XWsSession(this, _sessionListener, _log);
        }
    }
    class WsSessionNetwork : ISessionNetwork
    {
        private SessionOption _sessionOption;
        private ISessionListener _sessionListener;
        private WsSessionServer _wsSessionServer;
        private ILogger _log;
        public WsSessionNetwork(SessionOption sessionOption, ISessionListener sessionListener, ILogger log)
        {
            _sessionOption = sessionOption;
            _sessionListener = sessionListener;
            _log = log;

            _wsSessionServer = new WsSessionServer(IpFinder.FindLocalIp(), sessionOption.SessionPort, sessionListener, log);
        }
        public void Restart()
        {
            _log.Info("WsSessionNetwork Restart", typeof(WsSessionNetwork).Name);
            _wsSessionServer.Restart();
        }

        public void Start()
        {
            _log.Info("WsSessionNetwork Start", typeof(WsSessionNetwork).Name);
            _wsSessionServer.Start();
        }

        public void Stop()
        {
            _log.Info("WsSessionNetwork Stop", typeof(WsSessionNetwork).Name);
            _wsSessionServer.Stop();
        }
    }
}

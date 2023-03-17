using NetCoreServer;
using PlayHouse.Communicator.Message.buffer;
using PlayHouse.Communicator.Message;
using PlayHouse.Communicator;
using System.Net.Sockets;

namespace PlayHouse.Service.Session.network.websocket
{
    class XWsSession : WsSession
    {
        private PacketParser _packetParser;
        private ISessionListener _sessionListener;

        public XWsSession(WsSessionServer server, ISessionListener sessionListener) : base(server)
        {
            _packetParser = new PacketParser();
            _sessionListener = sessionListener;
        }

        private int GetSid()
        {
            return (int)Socket.Handle;
        }
        protected override void OnConnected()
        {

            LOG.Info($"Websocket session with Id {GetSid()} connected!", this.GetType());
            _sessionListener.OnConnect(GetSid());

        }

        protected override void OnDisconnected()
        {
            LOG.Info($"Websocket session with Id {GetSid()} disconnected!", this.GetType());
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
            LOG.Error($"Chat TCP session caught an error with code {error}", this.GetType());
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
            return new XWsSession(this, _sessionListener);
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
            LOG.Info("WsSessionNetwork Restart", this.GetType());
            _wsSessionServer.Restart();
        }

        public void Start()
        {
            LOG.Info("WsSessionNetwork Start", this.GetType());
            _wsSessionServer.Start();
        }

        public void Stop()
        {
            LOG.Info("WsSessionNetwork Stop", this.GetType());
            _wsSessionServer.Stop();
        }
    }
}

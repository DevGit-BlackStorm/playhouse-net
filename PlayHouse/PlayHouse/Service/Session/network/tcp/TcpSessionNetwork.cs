using NetCoreServer;
using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using PlayHouse.Communicator.Message.buffer;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PlayHouse.Service.Session.network.tcp
{
    class XTcpSession : TcpSession, ISession
    {
        private PacketParser _packetParser;
        private ISessionListener _sessionListener;
        private PooledBuffer _pooledBuffer;

        public XTcpSession(TcpServer server,ISessionListener sessionListener) : base(server)
        {
            _packetParser = new PacketParser();
            _sessionListener = sessionListener;
            _pooledBuffer = new PooledBuffer(ConstOption.SessionBufferSize);
        }

        private int GetSid()
        {
            return (int)Socket.Handle;
        }
        protected override void OnConnected()
        {
            
            LOG.Info($"TCP session with Id {GetSid()} connected!",this.GetType());
            _sessionListener.OnConnect(GetSid());

        }

        protected override void OnDisconnected()
        {
            LOG.Info($"TCP session with Id {GetSid()} disconnected!", this.GetType());
            _sessionListener.OnDisconnect(GetSid());
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            _pooledBuffer.Append(buffer, offset, size);
            List<ClientPacket>  packets = _packetParser.Parse(_pooledBuffer);
            foreach (ClientPacket packet in packets) {
                _sessionListener.OnReceive(GetSid(), packet);
            }
        }

        protected override void OnError(SocketError error)
        {
            LOG.Error($"Chat TCP session caught an error with code {error}", this.GetType());
            Disconnect();
        }

        public void ClientDisconnect()
        {
            base.Disconnect();
        }

        public void Send(ClientPacket packet)
        {
            //base.Send(packet.Data);
        }
    }
    public class TcpSessionServer : TcpServer
    {
        private ISessionListener _sessionListener;
        private ILogger _log;
        public TcpSessionServer(string address, int port,ISessionListener sessionListener,ILogger log) : base(address, port)
        {
            _sessionListener = sessionListener;
            _log = log;
        }

        protected override TcpSession CreateSession()
        {
            return new XTcpSession(this,_sessionListener);
        }
    }
    class TcpSessionNetwork : ISessionNetwork
    {
        private SessionOption _sessionOption;
        private ISessionListener _sessionListener;
        private TcpSessionServer _tcpSessionServer;
        private ILogger _log;
        public TcpSessionNetwork(SessionOption sessionOption,ISessionListener sessionListener,ILogger log) { 
            _sessionOption = sessionOption;
            _sessionListener = sessionListener;
            _log = log;

            _tcpSessionServer = new TcpSessionServer(IpFinder.FindLocalIp(), sessionOption.SessionPort, sessionListener, log);
        }
        public void Restart()
        {
            _log.Info("TcpSessionNetwork Restart", typeof(TcpSessionNetwork).Name);
            _tcpSessionServer.Restart();
        }

        public void Start()
        {
            _log.Info("TcpSessionNetwork Start", typeof(TcpSessionNetwork).Name);
            _tcpSessionServer.Start();
        }

        public void Stop()
        {
            _log.Info("TcpSessionNetwork Stop", typeof(TcpSessionNetwork).Name);
            _tcpSessionServer.Stop();
        }
    }
}

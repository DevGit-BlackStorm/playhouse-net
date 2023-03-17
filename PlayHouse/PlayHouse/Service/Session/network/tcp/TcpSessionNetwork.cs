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
        public TcpSessionServer(string address, int port,ISessionListener sessionListener) : base(address, port)
        {
            _sessionListener = sessionListener;
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
        public TcpSessionNetwork(SessionOption sessionOption,ISessionListener sessionListener) { 
            _sessionOption = sessionOption;
            _sessionListener = sessionListener;

            _tcpSessionServer = new TcpSessionServer(IpFinder.FindLocalIp(), sessionOption.SessionPort, sessionListener);
        }
        public void Restart()
        {
            LOG.Info("TcpSessionNetwork Restart", this.GetType());
            _tcpSessionServer.Restart();
        }

        public void Start()
        {
            LOG.Info("TcpSessionNetwork Start", this.GetType());
            _tcpSessionServer.Start();
        }

        public void Stop()
        {
            LOG.Info("TcpSessionNetwork Stop", this.GetType());
            _tcpSessionServer.Stop();
        }
    }
}

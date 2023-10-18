using CommonLib;
using NetCoreServer;
using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using PlayHouse.Production;
using PlayHouse.Production.Session;
using System.Net.Sockets;

namespace PlayHouse.Service.Session.network.tcp
{
    class XTcpSession : TcpSession, ISession
    {
        private PacketParser _packetParser;
        private ISessionListener _sessionListener;
        private RingBuffer _buffer = new RingBuffer(1024 * 8 ,1024*64*4);
        private RingBufferStream _stream;
            

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
                LOG.Info($"TCP session with Id {GetSid()} connected!", this.GetType());
                _sessionListener.OnConnect(GetSid(), this);
            }catch (Exception e)
            {
                LOG.Error(e.StackTrace,this.GetType(),e);
            }
        }

        protected override void OnDisconnected()
        {
            try
            {
                LOG.Info($"TCP session with Id {GetSid()} disconnected!", this.GetType());
                _sessionListener.OnDisconnect(GetSid());
            }
            catch (Exception e)
            {
                LOG.Error(e.StackTrace, this.GetType(), e);
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
                LOG.Error(e.StackTrace, this.GetType(), e);
            }
            
        }

        protected override void OnError(SocketError error)
        {
            try
            {
                LOG.Error($"Chat TCP session caught an error with code {error}", this.GetType());
                Disconnect();
            }
            catch(Exception e)
            {
                LOG.Error(e.StackTrace, this.GetType(), e);
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
    public class TcpSessionServer : TcpServer
    {
        private ISessionListener _sessionListener;
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
            LOG.Info("Server Started",GetType());
            
        }
    }
    class TcpSessionNetwork : ISessionNetwork
    {
        private TcpSessionServer _tcpSessionServer;
        
        public TcpSessionNetwork(SessionOption sessionOption,ISessionListener sessionListener) {

            //_tcpSessionServer = new TcpSessionServer(IpFinder.FindLocalIp(), sessionOption.SessionPort, sessionListener);
            _tcpSessionServer = new TcpSessionServer("0.0.0.0", sessionOption.SessionPort, sessionListener);
        }

        
        public void Start()
        {
            
            if (_tcpSessionServer.Start())
            {
                LOG.Info("TcpSessionNetwork Start", this.GetType());
            }
            else
            {
                LOG.Fatal("Session Server Start Fail", GetType());
                Environment.Exit(0);
            }
         
        }

        public void Stop()
        {
            LOG.Info("TcpSessionNetwork Stop", this.GetType());
            _tcpSessionServer.Stop();
        }
    }
}

using NetCoreServer;
using PlayHouse.Communicator.Message;
using PlayHouse.Communicator;
using System.Net.Sockets;
using CommonLib;
using PlayHouse.Production.Session;
using PlayHouse.Production;

namespace PlayHouse.Service.Session.network.websocket
{
    class XWsSession : WsSession, ISession
    {
        private PacketParser _packetParser;
        private ISessionListener _sessionListener;
        private RingBuffer _buffer = new RingBuffer(1024 * 8, 1024 * 64 * 4);
        private RingBufferStream _stream;

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
                LOG.Info($"Websocket session with Id {GetSid()} connected!", this.GetType());
                _sessionListener.OnConnect(GetSid(), this);
            }
            catch (Exception e)
            {
                LOG.Error(e.StackTrace, this.GetType(), e);
            }

          

        }

        protected override void OnDisconnected()
        {
            try
            {
                LOG.Info($"Websocket session with Id {GetSid()} disconnected!", this.GetType());
                _sessionListener.OnDisconnect(GetSid());
            }
            catch(Exception e)
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
            using(packet)
            {
                base.Send(packet.Data);
            }
        }
    }
    public class WsSessionServer : WsServer
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
        private WsSessionServer _wsSessionServer;
        public WsSessionNetwork(SessionOption sessionOption, ISessionListener sessionListener)
        {
            //_wsSessionServer = new WsSessionServer(IpFinder.FindLocalIp(), sessionOption.SessionPort, sessionListener);
            _wsSessionServer = new WsSessionServer("0.0.0.0", sessionOption.SessionPort, sessionListener);
            
        }
   
  
        public void Start()
        {
            
            if (_wsSessionServer.Start())
            {
                LOG.Info("WsSessionNetwork Start", this.GetType());
            }
            else
            {
                LOG.Fatal("WsSessionNetwork Start Fail", this.GetType());
                Environment.Exit(0);
            }
        }

        public void Stop()
        {
            LOG.Info("WsSessionNetwork Stop", this.GetType());
            _wsSessionServer.Stop();
        }
    }
}

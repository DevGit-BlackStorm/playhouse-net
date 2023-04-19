using PlayHouse.Communicator.Message;
using PlayHouse.Communicator;
using System.Collections.Concurrent;
using PlayHouse.Service.Session.network;

namespace PlayHouse.Service.Session
{
    public class SessionProcessor : IProcessor, ISessionListener
    {
        private readonly short _serviceId;
        private readonly SessionOption _sessionOption;
        private readonly IServerInfoCenter _serverInfoCenter;
        private readonly IClientCommunicator _clientCommunicator;
        private readonly RequestCache _requestCache;
        private readonly int _sessionPort;
        private readonly bool _showQps;

        private readonly ConcurrentDictionary<int, SessionClient> _clients = new ConcurrentDictionary<int, SessionClient>();
        private readonly AtomicEnumWrapper<ServerState> _state = new AtomicEnumWrapper<ServerState>(ServerState.DISABLE);
        private readonly SessionNetwork _sessionNetwork;
        private readonly PerformanceTester _performanceTester;
        private readonly ConcurrentQueue<(int, ClientPacket)> _clientQueue = new ConcurrentQueue<(int, ClientPacket)>();
        private readonly ConcurrentQueue<RoutePacket> _serverQueue = new ConcurrentQueue<RoutePacket>();
        private Thread? _clientMessageLoopThread;
        private Thread? _serverMessageLoopThread;

        public SessionProcessor(short serviceId, SessionOption sessionOption, IServerInfoCenter serverInfoCenter,
                               IClientCommunicator clientCommunicator, RequestCache requestCache, int sessionPort, bool showQps)
        {
            this._serviceId = serviceId;
            this._sessionOption = sessionOption;
            this._serverInfoCenter = serverInfoCenter;
            this._clientCommunicator = clientCommunicator;
            this._requestCache = requestCache;
            this._sessionPort = sessionPort;
            this._showQps = showQps;

            _sessionNetwork = new SessionNetwork(sessionOption, this);
            _performanceTester = new PerformanceTester(showQps, "client");
        }

        public void OnStart()
        {
            _state.Value = ServerState.RUNNING;
            _performanceTester.Start();

            _sessionNetwork.Start();

            _clientMessageLoopThread = new Thread(ClientMessageLoop) { Name = "session:client-message-loop" };
            _clientMessageLoopThread.Start();

            _serverMessageLoopThread = new Thread(ServerMessageLoop) { Name = "session:server-message-loop" };
            _serverMessageLoopThread.Start();
            
        }

        private void ClientMessageLoop()
        {
            while (_state.Value != ServerState.DISABLE)
            {
                while (_clientQueue.TryDequeue(out var message))
                {
                    var sessionId = message.Item1;
                    var clientPacket = message.Item2;

                    using (clientPacket)
                    {
                        LOG.Debug($"SessionService:onReceive {clientPacket.GetMsgId()} : from client", this.GetType());
                        if (!_clients.TryGetValue(sessionId, out var sessionClient))
                        {
                            LOG.Error($"sessionId is not exist {sessionId},{clientPacket.GetMsgId()}", this.GetType());
                        }
                        else
                        {
                            sessionClient.OnReceive(clientPacket);
                        }
                    }
                }
                Thread.Sleep(ConstOption.ThreadSleep);
            }
        }

        private void ServerMessageLoop()
        {
            while (_state.Value != ServerState.DISABLE)
            {
                while (_serverQueue.TryDequeue(out var routePacket))
                {
                    using (routePacket)
                    {
                        var sessionId = routePacket.RouteHeader.Sid;
                        var packetName = routePacket.GetMsgId();
                        if (!_clients.TryGetValue(sessionId, out var sessionClient))
                        {
                            LOG.Error($"sessionId is already disconnected  {sessionId},{packetName}", this.GetType());
                        }
                        else
                        {
                            sessionClient.OnReceive(routePacket);
                        }
                    }
                }
                Thread.Sleep(ConstOption.ThreadSleep);
            }
        }

        public void OnReceive(RoutePacket routePacket)
        {
            _serverQueue.Enqueue(routePacket);
        }

        public void OnStop()
        {
            _performanceTester.Stop();
            _state.Value = ServerState.DISABLE;
            _sessionNetwork.Stop();
        }

        public int GetWeightPoint()
        {
            return _clients.Count;
        }

        public ServerState GetServerState()
        {
            return _state.Value;
        }

        public ServiceType GetServiceType()
        {
            return ServiceType.SESSION;
        }

        public short GetServiceId()
        {
            return _serviceId;
        }

        public void Pause()
        {
            _state.Value = ServerState.PAUSE;
        }

        public void Resume()
        {
            _state.Value = ServerState.RUNNING;
        }

        public void OnConnect(int sid,ISession session)
        {
            if (!_clients.ContainsKey(sid))
            {
                _clients[sid] = new SessionClient(
                    _serviceId,
                    sid,
                    _serverInfoCenter,
                    session,
                    _clientCommunicator,
                    _sessionOption.Urls,
                    _requestCache);
            }
            else
            {
                LOG.Error($"sessionId is exist {sid}", this.GetType());
            }
        }

        public void OnReceive(int sid, ClientPacket clientPacket)
        {
            _clientQueue.Enqueue((sid, clientPacket));
        }

        public void OnDisconnect(int sid)
        {
            if (_clients.TryGetValue(sid, out var sessionClient))
            {
                sessionClient.Disconnect();
                _clients.TryRemove(sid, out _);
            }
            else
            {
                LOG.Error($"sessionId is not exist {sid}", this.GetType());
            }
        }
        
    }

}

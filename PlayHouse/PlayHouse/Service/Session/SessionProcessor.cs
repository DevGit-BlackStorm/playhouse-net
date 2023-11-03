using PlayHouse.Communicator.Message;
using PlayHouse.Communicator;
using System.Collections.Concurrent;
using PlayHouse.Service.Session.Network;
using PlayHouse.Utils;
using PlayHouse.Production;
using PlayHouse.Production.Session;

namespace PlayHouse.Service.Session
{
    public class SessionProcessor : IProcessor, ISessionListener
    {
        private readonly LOG<SessionProcessor> _log = new ();
        private readonly ushort _serviceId;
        private readonly SessionOption _sessionOption;
        private readonly IServerInfoCenter _serverInfoCenter;
        private readonly IClientCommunicator _clientCommunicator;
        private readonly RequestCache _requestCache;

        private readonly ConcurrentDictionary<int, SessionClient> _clients = new();
        private readonly AtomicEnum<ServerState> _state = new(ServerState.DISABLE);
        private readonly SessionNetwork _sessionNetwork;
        private readonly PerformanceTester _performanceTester;
        private readonly ConcurrentQueue<(int, ClientPacket)> _clientQueue = new();
        private readonly ConcurrentQueue<RoutePacket> _serverQueue = new();
        private Thread? _clientMessageLoopThread;
        private Thread? _serverMessageLoopThread;

        public ushort ServiceId => _serviceId;
        

        public SessionProcessor(ushort serviceId, SessionOption sessionOption, IServerInfoCenter serverInfoCenter,
                               IClientCommunicator clientCommunicator, RequestCache requestCache, int sessionPort, bool showQps)
        {
            _serviceId = serviceId;
            _sessionOption = sessionOption;
            _serverInfoCenter = serverInfoCenter;
            _clientCommunicator = clientCommunicator;
            _requestCache = requestCache;

            _sessionNetwork = new SessionNetwork(sessionOption, this);
            _performanceTester = new PerformanceTester(showQps, "client");
        }

        public void OnStart()
        {
            _state.Set(ServerState.RUNNING);
            _performanceTester.Start();

            _sessionNetwork.Start();

            _clientMessageLoopThread = new Thread(ClientMessageLoop) { Name = "session:client-message-loop" };
            _clientMessageLoopThread.Start();

            _serverMessageLoopThread = new Thread(ServerMessageLoop) { Name = "session:server-message-loop" };
            _serverMessageLoopThread.Start();
            
        }

        private void ClientMessageLoop()
        {
            while (_state.Get() != ServerState.DISABLE)
            {
                while (_clientQueue.TryDequeue(out var message))
                {
                    var sessionId = message.Item1;
                    var clientPacket = message.Item2;

                    using (clientPacket)
                    {
                        //LOG.Trace(()=>$"OnRecevie From Client: {clientPacket.Header}", this.GetType());
                        if (!_clients.TryGetValue(sessionId, out var sessionClient))
                        {
                            _log.Error(()=>$"sessionId is not exist - [sessionId:{sessionId},packetInfo:{clientPacket.Header}]");
                        }
                        else
                        {
                            sessionClient.Dispatch(clientPacket);
                        }
                    }
                }
                Thread.Sleep(ConstOption.ThreadSleep);
            }
        }

        private void ServerMessageLoop()
        {
            while (_state.Get() != ServerState.DISABLE)
            {
                while (_serverQueue.TryDequeue(out var routePacket))
                {
                    using (routePacket)
                    {
                        var sessionId = routePacket.RouteHeader.Sid;
                        if (!_clients.TryGetValue(sessionId, out var sessionClient))
                        {
                            var result = routePacket;
                            _log.Error(()=>$"sessionId is already disconnected - [sessionId:{sessionId},packetInfo:{result.RouteHeader}]");
                        }
                        else
                        {
                            sessionClient.Send(RoutePacket.MoveOf(routePacket));
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
            _state.Set(ServerState.DISABLE);
            _sessionNetwork.Stop();
        }

        public int GetWeightPoint()
        {
            return _clients.Count;
        }

        public ServerState GetServerState()
        {
            return _state.Get();
        }

        public ServiceType GetServiceType()
        {
            return ServiceType.SESSION;
        }

        public ushort GetServiceId()
        {
            return _serviceId;
        }

        public void Pause()
        {
            _state.Set(ServerState.PAUSE);
        }

        public void Resume()
        {
            _state.Set(ServerState.RUNNING);
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
                _log.Error(()=>$"sessionId is exist - [sid:{sid}]");
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
                _log.Error(()=>$"sessionId is not exist - [sid:{sid}]");
            }
        }
        
    }

}

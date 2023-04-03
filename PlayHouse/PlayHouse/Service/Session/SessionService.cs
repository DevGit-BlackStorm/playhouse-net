using PlayHouse.Communicator.Message;
using PlayHouse.Communicator;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Channels;
using PlayHouse.Service.Session.network;

namespace PlayHouse.Service.Session
{
    public class SessionService : IService, ISessionListener
    {
        private readonly short serviceId;
        private readonly SessionOption sessionOption;
        private readonly IServerInfoCenter serverInfoCenter;
        private readonly IClientCommunicator clientCommunicator;
        private readonly RequestCache requestCache;
        private readonly int sessionPort;
        private readonly bool showQps;

        private readonly ConcurrentDictionary<int, SessionClient> clients = new ConcurrentDictionary<int, SessionClient>();
        private readonly AtomicEnumWrapper<ServerState> state = new AtomicEnumWrapper<ServerState>(ServerState.DISABLE);
        private readonly SessionNetwork sessionNetwork;
        private readonly PerformanceTester performanceTester;
        private readonly ConcurrentQueue<(int, ClientPacket)> clientQueue = new ConcurrentQueue<(int, ClientPacket)>();
        private readonly ConcurrentQueue<RoutePacket> serverQueue = new ConcurrentQueue<RoutePacket>();
        private Thread? clientMessageLoopThread;
        private Thread? serverMessageLoopThread;

        public SessionService(short serviceId, SessionOption sessionOption, IServerInfoCenter serverInfoCenter,
                               IClientCommunicator clientCommunicator, RequestCache requestCache, int sessionPort, bool showQps)
        {
            this.serviceId = serviceId;
            this.sessionOption = sessionOption;
            this.serverInfoCenter = serverInfoCenter;
            this.clientCommunicator = clientCommunicator;
            this.requestCache = requestCache;
            this.sessionPort = sessionPort;
            this.showQps = showQps;

            sessionNetwork = new SessionNetwork(sessionOption, this);
            performanceTester = new PerformanceTester(showQps, "client");
        }

        public void OnStart()
        {
            state.Value = ServerState.RUNNING;
            performanceTester.Start();

            sessionNetwork.Start();

            clientMessageLoopThread = new Thread(ClientMessageLoop) { Name = "session:client-message-loop" };
            clientMessageLoopThread.Start();

            serverMessageLoopThread = new Thread(ServerMessageLoop) { Name = "session:server-message-loop" };
            serverMessageLoopThread.Start();
        }

        private void ClientMessageLoop()
        {
            while (state.Value != ServerState.DISABLE)
            {
                while (clientQueue.TryDequeue(out var message))
                {
                    var sessionId = message.Item1;
                    var clientPacket = message.Item2;

                    using (clientPacket)
                    {
                        LOG.Debug($"SessionService:onReceive {clientPacket.GetMsgId()} : from client", this.GetType());
                        if (!clients.TryGetValue(sessionId, out var sessionClient))
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
            while (state.Value != ServerState.DISABLE)
            {
                while (serverQueue.TryDequeue(out var routePacket))
                {
                    using (routePacket)
                    {
                        var sessionId = routePacket.RouteHeader.Sid;
                        var packetName = routePacket.GetMsgId();
                        if (!clients.TryGetValue(sessionId, out var sessionClient))
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
            serverQueue.Enqueue(routePacket);
        }

        public void OnStop()
        {
            performanceTester.Stop();
            state.Value = ServerState.DISABLE;
            sessionNetwork.Stop();
        }

        public int GetWeightPoint()
        {
            return clients.Count;
        }

        public ServerState GetServerState()
        {
            return state.Value;
        }

        public ServiceType GetServiceType()
        {
            return ServiceType.SESSION;
        }

        public short GetServiceId()
        {
            return serviceId;
        }

        public void Pause()
        {
            state.Value = ServerState.PAUSE;
        }

        public void Resume()
        {
            state.Value = ServerState.RUNNING;
        }

        public void OnConnect(int sid,ISession session)
        {
            if (!clients.ContainsKey(sid))
            {
                clients[sid] = new SessionClient(
                    serviceId,
                    sid,
                    serverInfoCenter,
                    session,
                    clientCommunicator,
                    sessionOption.Urls,
                    requestCache);
            }
            else
            {
                LOG.Error($"sessionId is exist {sid}", this.GetType());
            }
        }

        public void OnReceive(int sid, ClientPacket clientPacket)
        {
            clientQueue.Enqueue((sid, clientPacket));
        }

        public void OnDisconnect(int sid)
        {
            if (clients.TryGetValue(sid, out var sessionClient))
            {
                sessionClient.Disconnect();
                clients.TryRemove(sid, out _);
            }
            else
            {
                LOG.Error($"sessionId is not exist {sid}", this.GetType());
            }
        }
        
    }

}

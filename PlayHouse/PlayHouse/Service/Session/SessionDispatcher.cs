using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using PlayHouse.Production.Session;
using PlayHouse.Production.Shared;
using PlayHouse.Service.Session.Network;
using PlayHouse.Utils;
using System.Collections.Concurrent;

namespace PlayHouse.Service.Session
{
    internal class SessionDispatcher : ISessionListener
    {
        private readonly LOG<SessionDispatcher> _log = new();
        private readonly ushort _serviceId;
        private readonly SessionOption _sessionOption;
        private readonly IServerInfoCenter _serverInfoCenter;
        private readonly IClientCommunicator _clientCommunicator;
        private readonly RequestCache _requestCache;
        private readonly SessionNetwork _sessionNetwork;
        private readonly ConcurrentDictionary<int, SessionActor> _sessionActors = new();
        private readonly Timer _timer;

        public SessionDispatcher(
            ushort serviceId, 
            SessionOption sessionOption, 
            IServerInfoCenter serverInfoCenter, 
            IClientCommunicator clientCommunicator, 
            RequestCache requestCache)
        {
            _serviceId = serviceId;
            _sessionOption = sessionOption;
            _serverInfoCenter = serverInfoCenter;
            _clientCommunicator = clientCommunicator;
            _requestCache = requestCache;

            _sessionNetwork = new SessionNetwork(sessionOption, this);

            _timer = new Timer(TimerCallback, this, 1000, 1000);
        }

        public void Start()  
        {
            _sessionNetwork.Start();
        }

        public void Stop()
        {
            _sessionNetwork.Stop();
            _timer.Dispose();
        }


         private static void TimerCallback(Object? o)
        {
            SessionDispatcher dispacher = (SessionDispatcher)o!;
            // 여기에 타이머 만료 시 실행할 코드 작성
            var keysToRemove =
                dispacher._sessionActors.Where(k => k.Value.IsIdleState(dispacher._sessionOption.ClientIdleTimeoutMSec)).Select(k => k.Key);

            foreach (var key in keysToRemove)
            {
                SessionActor? client;
                dispacher._sessionActors.Remove(key, out client);
                if (client != null)
                {

                    dispacher._log.Debug(() => $"idle client disconnect - [sid:{client.Sid},accountId:{client.AccountId},idleTime:{client.IdleTime()}]");
                    client.ClientDisconnect();
                }
            }
        }


        public async Task DispatchAsync(RoutePacket routePacket)
        {
            using(routePacket)
            {
                var sessionId = routePacket.RouteHeader.Sid;
                if (!_sessionActors.TryGetValue(sessionId, out var sessionClient))
                {
                    var result = routePacket;
                    _log.Error(() => $"sessionId is already disconnected - [sessionId:{sessionId},packetInfo:{result.RouteHeader}]");
                }
                else
                {
                    await sessionClient.PostAsync(RoutePacket.MoveOf(routePacket));
                }

                await Task.CompletedTask;
            }
        }

        private async  Task DispatchAsync(int sessionId, ClientPacket clientPacket) 
        {
            using(clientPacket)
            {
                if (!_sessionActors.TryGetValue(sessionId, out var sessionClient))
                {
                    _log.Debug(() => $"sessionId is not exist - [sessionId:{sessionId},packetInfo:{clientPacket.Header}]");
                }
                else
                {
                    await sessionClient.DispatchAsync(clientPacket);
                }
            }
        }

        public void OnConnect(int sid, ISession session)
        {
            if (!_sessionActors.ContainsKey(sid))
            {
                _sessionActors[sid] = new SessionActor(
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
                _log.Error(() => $"sessionId is exist - [sid:{sid}]");
            }
        }

        public void OnDisconnect(int sid)
        {
            if (_sessionActors.TryGetValue(sid, out var sessionClient))
            {
                sessionClient.Disconnect();
                _sessionActors.TryRemove(sid, out _);
            }
            else
            {
                _log.Debug(() => $"sessionId is not exist - [sid:{sid}]");
            }
        }

        public void OnReceive(int sid, ClientPacket clientPacket)
        {
            Task.Run(async () => { await DispatchAsync(sid, clientPacket); });
        }

        internal int GetActorCount()
        {
            return _sessionActors.Count;
        }
    }
}

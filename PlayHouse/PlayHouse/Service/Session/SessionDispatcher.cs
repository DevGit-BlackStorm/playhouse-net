using System.Collections.Concurrent;
using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using PlayHouse.Production.Session;
using PlayHouse.Production.Shared;
using PlayHouse.Service.Session.Network;
using PlayHouse.Utils;

namespace PlayHouse.Service.Session;

internal class SessionDispatcher : ISessionListener
{
    private readonly IClientCommunicator _clientCommunicator;
    private readonly LOG<SessionDispatcher> _log = new();
    private readonly RequestCache _requestCache;
    private readonly IServerInfoCenter _serverInfoCenter;
    private readonly ushort _serviceId;
    private readonly ConcurrentDictionary<long, SessionActor> _sessionActors = new();
    private readonly SessionNetwork _sessionNetwork;
    private readonly SessionOption _sessionOption;
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

    public void OnConnect(long sid, ISession session,string remoteIp)
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
                _requestCache,
                remoteIp
                );
        }
        else
        {
            _log.Error(() => $"sessionId is exist - [sid:{sid}]");
        }
    }

    public void OnDisconnect(long sid)
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

    public void OnReceive(long sid, ClientPacket clientPacket)
    {
        Dispatch(sid, clientPacket);
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


    private static void TimerCallback(object? o)
    {
        //todo 임시로 idle disconnect 끔
        //var dispacher = (SessionDispatcher)o!;
        // 여기에 타이머 만료 시 실행할 코드 작성
        //var keysToRemove =
        //    dispacher._sessionActors.Where(k => k.Value.IsIdleState(dispacher._sessionOption.ClientIdleTimeoutMSec))
        //        .Select(k => k.Key).ToList();

        //foreach (var key in keysToRemove)
        //{
        //    SessionActor? client;
        //    dispacher._sessionActors.Remove(key, out client);
        //    if (client != null)
        //    {
        //        dispacher._log.Debug(() =>
        //            $"idle client disconnect - [sid:{client.Sid},accountId:{client.AccountId},idleTime:{client.IdleTime()}]");
        //        client.ClientDisconnect();
        //    }
        //}
    }


    private void Dispatch(long sessionId, ClientPacket clientPacket)
    {
        using (clientPacket)
        {
            if (!_sessionActors.TryGetValue(sessionId, out var sessionClient))
            {
                _log.Debug(() => $"sessionId is not exist - [sessionId:{sessionId},packetInfo:{clientPacket.Header}]");
            }
            else
            {
                sessionClient.Dispatch(clientPacket);
            }
        }
    }

    internal int GetActorCount()
    {
        return _sessionActors.Count;
    }

    internal void OnPost(RoutePacket routePacket)
    {
        using (routePacket)
        {
            var sessionId = routePacket.RouteHeader.Sid;
            if (!_sessionActors.TryGetValue(sessionId, out var sessionClient))
            {
                var result = routePacket;
                _log.Error(() =>
                    $"sessionId is already disconnected - [sessionId:{sessionId},packetInfo:{result.RouteHeader}]");
            }
            else
            {
                sessionClient.Post(RoutePacket.MoveOf(routePacket));
            }
        }
    }
}